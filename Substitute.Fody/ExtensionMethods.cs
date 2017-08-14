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
                    attr => moduleDefinition.ImportReference((TypeReference)attr.ConstructorArguments[1].Value).Resolve(),
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

        [CanBeNull]
        public static string GetSignature([CanBeNull] this IMemberDefinition member, [CanBeNull] TypeDefinition targetType = null)
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

                    throw new WeavingException(
$@"{source} => {target} substitution error. {source} derives from {sourceBase}, but there is no direct or substituted counterpart for {sourceBase} in the targets base classes.
Either derive {target} from {sourceBase}, or substitute {sourceBase} with {target} or one of it's base classes."
                        
                        , target);
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
