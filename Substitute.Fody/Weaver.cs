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
            [NotNull] private readonly ModuleDefinition _moduleDefinition;
            [NotNull] private readonly HashSet<TypeReference> _validatedTypes = new HashSet<TypeReference>(TypeReferenceEqualityComparer.Default);
            [NotNull] private readonly IDictionary<TypeReference, TypeDefinition> _substitutionMap;

            public Weaver([NotNull] ModuleDefinition moduleDefinition)
            {
                _moduleDefinition = moduleDefinition;
                _substitutionMap = moduleDefinition.CreateSubstitutionMap().Validate();
            }

            internal void Weave()
            {
                // ReSharper disable once PossibleNullReferenceException
                foreach (var type in _moduleDefinition.GetTypes())
                {
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

                        foreach (var variable in method.Body.Variables)
                        {
                            variable.VariableType = GetSubstitute(variable.VariableType);
                        }

                        var instructions = method.Body.Instructions;

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
                                    instructions[i] = Instruction.Create(inst.OpCode, substitute.Find(operandMethod.Resolve()));
                                }
                            }
                            else if (inst.Operand is FieldReference operandField)
                            {
                                if (TryGetSubstitute(operandField.DeclaringType, out var substitute))
                                {
                                    instructions[i] = Instruction.Create(inst.OpCode, substitute.Find(operandField.Resolve()));
                                }
                            }
                        }
                    }
                }
            }

            [NotNull]
            TypeReference GetSubstitute([NotNull] TypeReference type)
            {
                if (_substitutionMap.TryGetValue(type, out var substitute))
                {
                    return substitute;
                }

                if (_validatedTypes.Contains(type))
                    return type;

                var substitutedBaseType = type.GetBaseTypes().FirstOrDefault(baseType => _substitutionMap.ContainsKey(baseType));

                if (substitutedBaseType != null)
                    throw new WeavingException($"{type} is not substituted, but is derived from the substituted type {substitutedBaseType}. You must substitute {type}, too.", type);

                _validatedTypes.Add(type);
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
        }
    }
}