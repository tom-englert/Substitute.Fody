using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Substitute
{
    internal static class WeaverExtensions
    {
        internal static void Weave([NotNull] this ModuleDefinition moduleDefinition, [NotNull] ILogger logger)

        {
            try
            {
                new Weaver(moduleDefinition).Weave();
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
            private readonly IDictionary<TypeReference, TypeDefinition> _substitutionMap;
            [NotNull, ItemNotNull]
            private readonly HashSet<TypeReference> _substitutes;
            [NotNull]
            private readonly IDictionary<TypeReference, Exception> _unmappedTypeErrors;

            public Weaver([NotNull] ModuleDefinition moduleDefinition)
            {
                _moduleDefinition = moduleDefinition;
                _substitutionMap = moduleDefinition.CreateSubstitutionMap();
                _substitutes = new HashSet<TypeReference>(_substitutionMap.Values, TypeReferenceEqualityComparer.Default);
                _unmappedTypeErrors = _substitutionMap.GetUnmappedTypeErrors();
            }

            internal void Weave()
            {
                // ReSharper disable once PossibleNullReferenceException
                foreach (var type in _moduleDefinition.GetTypes())
                {
                    // avoid recursions...
                    if (_substitutionMap.ContainsKey(type) || _substitutes.Contains(type))
                        continue;

                    foreach (var genericParameter in type.GenericParameters)
                    {
                        genericParameter.Constraints.ReplaceItems(GetSubstitute);
                    }

                    foreach (var field in type.Fields)
                    {
                        field.FieldType = GetSubstitute(field.FieldType);
                    }

                    foreach (var property in type.Properties)
                    {
                        property.PropertyType = GetSubstitute(property.PropertyType);
                    }

                    foreach (var method in type.Methods)
                    {
                        foreach (var genericParameter in method.GenericParameters)
                        {
                            genericParameter.Constraints.ReplaceItems(GetSubstitute);
                        }

                        method.ReturnType = GetSubstitute(method.ReturnType);

                        var methodBody = method.Body;

                        if (methodBody == null)
                            continue;

                        foreach (var variable in methodBody.Variables)
                        {
                            variable.VariableType = GetSubstitute(variable.VariableType);
                        }

                        var instructions = methodBody.Instructions;

                        for (var i = 0; i < instructions.Count; i++)
                        {
                            var inst = instructions[i];
                            if (inst.Operand is TypeReference operandType)
                            {
                                if (TryGetSubstitute(operandType, out var substitute))
                                {
                                    instructions[i] = Instruction.Create(inst.OpCode, substitute);
                                }
                            }
                            else if (inst.Operand is MethodReference operandMethod)
                            {
                                if (TryGetSubstitute(operandMethod.DeclaringType, out var substitute))
                                {
                                    instructions[i] = Instruction.Create(inst.OpCode, Find(substitute, operandMethod.Resolve()));
                                }
                            }
                            else if (inst.Operand is FieldReference operandField)
                            {
                                if (TryGetSubstitute(operandField.DeclaringType, out var substitute))
                                {
                                    instructions[i] = Instruction.Create(inst.OpCode, Find(substitute, operandField.Resolve()));
                                }
                            }
                        }
                    }
                }
            }

            [NotNull]
            private TypeReference GetSubstitute([NotNull] TypeReference type)
            {
                if (_substitutionMap.TryGetValue(type, out var substitute))
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    return _moduleDefinition.ImportReference(substitute);
                }

                if (_validatedTypes.Add(type))
                {
                    if (_unmappedTypeErrors.TryGetValue(type, out var error))
                        throw error;

                    var substitutedBaseType = type.GetBaseTypes().FirstOrDefault(baseType => _substitutionMap.ContainsKey(baseType));

                    if (substitutedBaseType != null)
                        throw new WeavingException($"{type} is not substituted, but is derived from the substituted type {substitutedBaseType}. You must substitute {type}, too.", type);
                }

                return type;
            }

            [ContractAnnotation("substitute:null => false")]
            private bool TryGetSubstitute([NotNull] TypeReference type, [CanBeNull] out TypeDefinition substitute)
            {
                var t = GetSubstitute(type);
                substitute = null;

                if (t != type)
                {
                    substitute = t.Resolve();
                }

                return substitute != null;
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