using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Substitute
{
    using ISubstitutionMap = IReadOnlyDictionary<TypeReference, SubstitutionTarget>;

    internal static class WeaverExtensions
    {
        internal static void Weave([NotNull] this ModuleDefinition moduleDefinition, [NotNull] ILogger logger, Parameters defaultParameters)
        {
            try
            {
                new Weaver(moduleDefinition, defaultParameters).Weave();
            }
            catch (WeavingException ex)
            {
                logger.LogError(ex.Message, ex.Type?.TryGetSequencePoint());
            }
        }

        private class Weaver
        {
            [NotNull]
            private readonly ModuleDefinition _moduleDefinition;
            [NotNull, ItemNotNull]
            private readonly HashSet<TypeReference> _validatedTypes = new HashSet<TypeReference>(TypeReferenceEqualityComparer.Default);
            [NotNull]
            private readonly IReadOnlyDictionary<TypeReference, SubstitutionTarget> _substitutionMap;
            [NotNull]
            private readonly IDictionary<TypeReference, Exception> _unmappedTypeErrors;
            private readonly Parameters _defaultParameters;

            public Weaver([NotNull] ModuleDefinition moduleDefinition, Parameters defaultParameters)
            {
                _moduleDefinition = moduleDefinition;
                _defaultParameters = defaultParameters;
                var assemblySubstitutionMap = moduleDefinition.Assembly.CreateSubstitutionMap(defaultParameters);
                _substitutionMap = moduleDefinition.CreateSubstitutionMap(defaultParameters, assemblySubstitutionMap);
                var substitutes = new HashSet<TypeReference>(_substitutionMap.Values.Select(t => t.TargetType));
                _substitutionMap.CheckRecursions(substitutes);

                _unmappedTypeErrors = _substitutionMap.GetUnmappedTypeErrors();
            }

            internal void Weave()
            {
                // ReSharper disable once PossibleNullReferenceException
                foreach (var type in _moduleDefinition.Types)
                    SubstituteType(type, _substitutionMap);
            }

            private void SubstituteType([NotNull] TypeDefinition typeDef, [NotNull] ISubstitutionMap substitutionMap)
            {
                var typeSubstitution = typeDef.CreateSubstitutionMap(_defaultParameters, substitutionMap);
                var substitutes = CheckRecursion(typeSubstitution);

                // avoid recursions...
                if (typeSubstitution.ContainsKey(typeDef) || substitutes.Contains(typeDef))
                    return;

                if (typeDef.HasGenericParameters)
                    foreach (var genericParameter in typeDef.GenericParameters)
                        SubstituteSignature(genericParameter.Constraints, typeSubstitution);

                if (typeDef.HasFields)
                    foreach (var field in typeDef.Fields)
                        SubstituteField(field, typeSubstitution);

                var propertyMethods = new HashSet<MethodDefinition>();
                if (typeDef.HasProperties)
                    foreach (var property in typeDef.Properties)
                    {
                        SubstituteProperty(property, typeSubstitution);
                        if (property.GetMethod != null)
                            propertyMethods.Add(property.GetMethod);
                        if (property.SetMethod != null)
                            propertyMethods.Add(property.SetMethod);
                        if (property.HasOtherMethods)
                            propertyMethods.UnionWith(property.OtherMethods);

                    }

                if (typeDef.HasMethods)
                    foreach (var method in typeDef.Methods.Where(m => !propertyMethods.Contains(m)))
                        SubstituteMethod(method, typeSubstitution);

                if (typeDef.HasNestedTypes)
                    foreach (var nestedType in typeDef.NestedTypes)
                        SubstituteType(nestedType, typeSubstitution);
            }

            private static ISet<TypeReference> CheckRecursion(ISubstitutionMap substitutionMap)
            {
                var substitutes = new HashSet<TypeReference>(substitutionMap.Values.Select(t => t.TargetType));
                substitutionMap.CheckRecursions(substitutes);
                return substitutes;
            }

            private void SubstituteField([NotNull] FieldDefinition fieldDef, [NotNull] ISubstitutionMap substitutionMap)
            {
                var fieldSubstitutionMap = fieldDef.CreateSubstitutionMap(_defaultParameters, substitutionMap);
                if (!ReferenceEquals(substitutionMap, fieldSubstitutionMap))
                    CheckRecursion(fieldSubstitutionMap);

                fieldDef.FieldType = SubstituteSignature(fieldDef.FieldType, fieldSubstitutionMap);
            }

            private void SubstituteProperty([NotNull] PropertyDefinition propertyDef, [NotNull] ISubstitutionMap substitutionMap)
            {
                var propertySubstitutionMap = propertyDef.CreateSubstitutionMap(_defaultParameters, substitutionMap);
                if (!ReferenceEquals(substitutionMap, propertySubstitutionMap))
                    CheckRecursion(propertySubstitutionMap);

                propertyDef.PropertyType = SubstituteSignature(propertyDef.PropertyType, propertySubstitutionMap);

                if (propertyDef.GetMethod != null)
                    SubstituteMethod(propertyDef.GetMethod, propertySubstitutionMap);
                if (propertyDef.SetMethod != null)
                    SubstituteMethod(propertyDef.SetMethod, propertySubstitutionMap);
                if (propertyDef.HasOtherMethods)
                    foreach (var otherMethod in propertyDef.OtherMethods)
                        SubstituteMethod(otherMethod, propertySubstitutionMap);
            }

            private void SubstituteMethod([NotNull] MethodDefinition methodDef, [NotNull] ISubstitutionMap substitutionMap)
            {
                var methodSubstitutionMap = methodDef.CreateSubstitutionMap(_defaultParameters, substitutionMap);
                if (!ReferenceEquals(substitutionMap, methodSubstitutionMap))
                    CheckRecursion(methodSubstitutionMap);

                foreach (var genericParameter in methodDef.GenericParameters)
                    SubstituteSignature(genericParameter.Constraints, methodSubstitutionMap);

                methodDef.ReturnType = SubstituteSignature(methodDef.ReturnType, methodSubstitutionMap);

                if (methodDef.HasBody)
                {
                    if (methodDef.Body.HasVariables)
                        foreach (var variable in methodDef.Body.Variables)
                            variable.VariableType = Substitute(variable.VariableType, methodSubstitutionMap);

                    foreach (var instr in methodDef.Body.Instructions)
                    {
                        if (instr.Operand != null)
                        {
                            switch (instr.Operand)
                            {
                                case TypeReference typeRef:
                                    instr.Operand = Substitute(typeRef, methodSubstitutionMap);
                                    break;
                                case MethodReference methodRef:
                                    {
                                        if (TrySubstitute(methodRef.DeclaringType, methodSubstitutionMap, out var substituted))
                                            instr.Operand = Find(substituted.ResolveStrict(), methodRef.Resolve());
                                        break;
                                    }
                                case FieldReference fieldRef:
                                    {
                                        if (TrySubstitute(fieldRef.DeclaringType, methodSubstitutionMap, out var substituted))
                                            instr.Operand = Find(substituted.ResolveStrict(), fieldRef.Resolve());
                                    }
                                    break;
                            }
                        }
                    }
                }
            }

            private void SubstituteSignature([NotNull, ItemNotNull] IList<TypeReference> typeRefs, [NotNull] ISubstitutionMap substitutionMap)
            {
                for (var i = 0; i < typeRefs.Count; i++)
                    if (TrySubstituteSignature(typeRefs[i], substitutionMap, out var substituted))
                        typeRefs[i] = substituted;
            }

            private TypeReference SubstituteSignature([NotNull] TypeReference typeRef, [NotNull] ISubstitutionMap substitutionMap) =>
                TrySubstituteSignature(typeRef, substitutionMap, out var substituted) ? substituted : typeRef;

            private bool TrySubstituteSignature([NotNull] TypeReference typeRef, [NotNull] ISubstitutionMap substitutionMap, out TypeReference substituted) =>
                TrySubstitute(typeRef, substitutionMap, subst => !subst.Parameters.DoNotChangeSignature, out substituted);

            private TypeReference Substitute([NotNull] TypeReference typeRef, [NotNull] ISubstitutionMap substitutionMap) =>
                TrySubstitute(typeRef, substitutionMap, out var substituted) ? substituted : typeRef;

            private bool TrySubstitute([NotNull] TypeReference typeRef, [NotNull] ISubstitutionMap substitutionMap, out TypeReference substituted) =>
                TrySubstitute(typeRef, substitutionMap, subst => true, out substituted);

            [NotNull]
            private bool TrySubstitute([NotNull] TypeReference typeRef, [NotNull] ISubstitutionMap substitutionMap, [NotNull] Func<SubstitutionTarget, bool> doSubstitution, out TypeReference substituted)
            {
                if (substitutionMap.TryGetValue(typeRef, out var substitute))
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    if (doSubstitution.Invoke(substitute))
                    {
                        substituted = _moduleDefinition.ImportReference(substitute.TargetType);
                        return true;
                    }
                }

                if (typeRef is GenericInstanceType genericType)
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    for (var i = 0; i < genericType.GenericArguments.Count; i++)
                    {
                        if (TrySubstitute(genericType.GenericArguments[i], substitutionMap, doSubstitution, out var substitutedGeneric))
                            genericType.GenericArguments[i] = substitutedGeneric;
                    }
                }

                // ReSharper disable once PossibleNullReferenceException
                foreach (var genericParameter in typeRef.GenericParameters)
                    SubstituteSignature(genericParameter, substitutionMap);

                if (_validatedTypes.Add(typeRef))
                {
                    if (_unmappedTypeErrors.TryGetValue(typeRef, out var error))
                        throw error;

                    var substitutedBaseType = typeRef.GetBaseTypes().FirstOrDefault(baseType => _substitutionMap.ContainsKey(baseType));

                    if (substitutedBaseType != null)
                        throw new WeavingException($"{typeRef} is not substituted, but is derived from the substituted type {substitutedBaseType}. You must substitute {typeRef}, too.", typeRef);
                }

                substituted = null;
                return false;
            }

            [NotNull]
            private MethodReference Find([NotNull] TypeDefinition type, [NotNull] MethodDefinition template)
            {
                var signature = template.GetSignature(type);

                var newItem = type.Methods?.FirstOrDefault(m => m.GetSignature() == signature);

                if (newItem == null)
                {
                    throw new WeavingException($"The type {type} cannot substitute {template.DeclaringType}, because it does not contain a method {signature}.", type);
                }

                if (newItem.IsPrivate)
                {
                    throw new WeavingException($"The type {type} cannot substitute {template.DeclaringType}, because the method {signature} is private.", type);
                }

                // ReSharper disable once AssignNullToNotNullAttribute
                return _moduleDefinition.ImportReference(newItem);
            }

            [NotNull]
            private FieldReference Find([NotNull] TypeDefinition type, [NotNull] FieldDefinition template)
            {
                var signature = template.GetSignature(type);

                var newItem = type.Fields?.FirstOrDefault(m => m.GetSignature() == signature);

                if (newItem == null)
                {
                    throw new WeavingException($"The type {type} cannot substitute {template.DeclaringType}, because it does not contain a field {signature}.", type);
                }

                if (newItem.IsPrivate)
                {
                    throw new WeavingException($"The type {type} cannot substitute {template.DeclaringType}, because the field {signature} is private.", type);
                }

                // ReSharper disable once AssignNullToNotNullAttribute
                return _moduleDefinition.ImportReference(newItem);
            }
        }
    }
}