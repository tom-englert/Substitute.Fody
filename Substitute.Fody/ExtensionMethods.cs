using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;

using JetBrains.Annotations;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Substitute
{
    internal static class ExtensionMethods
    {
        [NotNull]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute"), SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public static IDictionary<TypeReference, TypeDefinition> CreateSubstitutionMap([NotNull] this ModuleDefinition moduleDefinition)
            => moduleDefinition.Assembly.CustomAttributes
                .Where(ca => ca.AttributeType?.FullName == "Substitute.SubstituteAttribute")
                .Where(attr => attr.ConstructorArguments.Count == 2)
                .Where(attr => attr.ConstructorArguments.All(ca => ca.Type.FullName == "System.Type"))
                .ToDictionary(
                    attr => (TypeReference)attr.ConstructorArguments[0].Value,
                    attr => moduleDefinition.MetadataResolver.Resolve((TypeReference)attr.ConstructorArguments[1].Value),
                    TypeReferenceEqualityComparer.Default);


        [NotNull, ItemNotNull]
        public static IEnumerable<TypeReference> GetBaseTypes([NotNull] this TypeReference type)
        {
            while ((type = type.Resolve()?.BaseType) != null)
            {
                yield return type;
            }
        }

        [NotNull, ItemNotNull]
        public static IEnumerable<TypeReference> GetSelfAndBaseTypes([NotNull] this TypeReference type)
        {
            yield return type;

            while ((type = type.Resolve()?.BaseType) != null)
            {
                yield return type;
            }
        }

        public static void ReplaceItems<T>([NotNull, ItemCanBeNull] this IList<T> items, [NotNull] Func<T, T> replace)
            where T : TypeReference
        {
            for (var i = 0; i < items.Count; i++)
            {
                items[i] = replace(items[i]);
            }
        }


        [NotNull]
        public static MethodReference Find([NotNull] this TypeDefinition type, [NotNull] MethodDefinition template)
        {
            var signature = template.GetSignature(type);

            var newItem = type.Methods?.FirstOrDefault(m => m.GetSignature() == signature);

            if (newItem == null)
            {
                throw new WeavingException("The type {type} does not contain a method {signature}", type);
            }

            if (newItem.IsPrivate)
            {
                throw new WeavingException($"The method {signature} on type {type} must not be private to substitute {template}", type);
            }

            return newItem;
        }

        [NotNull]
        public static FieldReference Find([NotNull] this TypeDefinition type, [NotNull] FieldDefinition template)
        {
            var signature = template.GetSignature(type);

            var newItem = type.Fields?.FirstOrDefault(m => m.GetSignature() == signature);

            if (newItem == null)
            {
                throw new WeavingException($"The type {type} does not contain a field {signature}", type);
            }

            if (newItem.IsPrivate)
            {
                throw new WeavingException($"The field {signature} on type {type} must not be private to substitute {template}", type);
            }

            return newItem;
        }

        [CanBeNull]
        private static string GetSignature([CanBeNull] this IMemberDefinition member, [CanBeNull] TypeDefinition targetType = null)
        {
            if (member == null)
                return null;

            var value = member.ToString();

            if (targetType != null)
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                // ReSharper disable once PossibleNullReferenceException
                value = value.Replace(member.DeclaringType.FullName, targetType.FullName);
            }

            if (true.Equals(((dynamic)member).IsStatic))
            {
                value = "static " + value;
            }

            return value;
        }

        [NotNull]
        public static IDictionary<TypeReference, TypeDefinition> Validate([NotNull] this IDictionary<TypeReference, TypeDefinition> substitutionMap)
        {
            foreach (var entry in substitutionMap)
            {
                var source = entry.Key;
                var target = entry.Value;

                Contract.Assume(source != null);
                Contract.Assume(target != null);

                var targetAndBases = target.GetSelfAndBaseTypes()
                    .Select((reference, index) => new {reference, index})
                    .ToDictionary(item => item.reference, item => item.index, TypeReferenceEqualityComparer.Default);

                if (targetAndBases.ContainsKey(source))
                    throw new WeavingException($"{source} => {target} substitution error. {source} cannot be substituted by itself.", target);

                var lastTargetIndex = 0;

                var sourceBase = source;

                while ((sourceBase = sourceBase.Resolve()?.BaseType) != null)
                {
                    Contract.Assume(sourceBase != null);

                    if (targetAndBases.TryGetValue(sourceBase, out var index) && (index > lastTargetIndex))
                    {
                        lastTargetIndex = index;
                        continue;
                    }

                    if (substitutionMap.TryGetValue(sourceBase, out var targetBase) && targetAndBases.TryGetValue(targetBase, out index) && (index >= lastTargetIndex))
                    {
                        lastTargetIndex = index;
                        continue;
                    }

                    throw new WeavingException($"{source} => {target} substitution error. There is no counterpart for {sourceBase} in the targets base classes", target);
                }
            }

            return substitutionMap;
        }

        [CanBeNull]
        public static SequencePoint TryGetSequencePoint([CanBeNull] this TypeReference type)
        {
            var definition = type?.Resolve();

            var method = definition?.Methods?.FirstOrDefault();

            return method == null ? null : definition.Module?.SymbolReader?.Read(method)?.SequencePoints?.FirstOrDefault();
        }
    }
}
