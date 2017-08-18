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
        public static IDictionary<TypeReference, TypeDefinition> CreateSubstitutionMap([NotNull] this ModuleDefinition moduleDefinition)
            => moduleDefinition.GetTypeMappings()
                .VerfifyNoDuplicates()
                .ToDictionary(item => item.Key, item => item.Value, TypeReferenceEqualityComparer.Default);

        [NotNull]
        private static IList<KeyValuePair<TypeReference, TypeDefinition>> VerfifyNoDuplicates([NotNull] this IList<KeyValuePair<TypeReference, TypeDefinition>> typeMappings)
        {
            var duplicateDefinition = typeMappings
                .GroupBy(item => item.Key, TypeReferenceEqualityComparer.Default)
                // ReSharper disable once AssignNullToNotNullAttribute
                .Where(group => @group.Count() > 1)
                .Select(group => @group.Key)
                .FirstOrDefault();

            if (duplicateDefinition != null)
                throw new WeavingException($"Duplicate substitution mapping for type {duplicateDefinition}.", duplicateDefinition);

            return typeMappings;
        }

        [NotNull]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute"), SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private static IList<KeyValuePair<TypeReference, TypeDefinition>> GetTypeMappings([NotNull] this ModuleDefinition moduleDefinition)
        {
            return moduleDefinition.Assembly.CustomAttributes
                .Where(ca => ca.AttributeType?.FullName == "Substitute.SubstituteAttribute")
                .Where(attr => attr.ConstructorArguments.Count == 2)
                .Where(attr => attr.ConstructorArguments.All(ca => ca.Type.FullName == "System.Type"))
                .Select(attr => new KeyValuePair<TypeReference, TypeDefinition>((TypeReference)attr.ConstructorArguments[0].Value, moduleDefinition.ImportReference((TypeReference)attr.ConstructorArguments[1].Value).Resolve()))
                .ToArray();
        }


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

        [NotNull, ItemNotNull]
        public static IEnumerable<TypeReference> GetAllInterfaces([NotNull] this TypeReference type)
        {
            return type.GetSelfAndBaseTypes().SelectMany(t => t.Resolve().Interfaces.Select(i => i.InterfaceType));
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
        public static IDictionary<TypeReference, Exception> GetUnmappedTypeErrors([NotNull] this IDictionary<TypeReference, TypeDefinition> substitutionMap)
        {
            var invalidTypes = new Dictionary<TypeReference, Exception>(TypeReferenceEqualityComparer.Default);

            foreach (var entry in substitutionMap)
            {
                var source = entry.Key;
                var target = entry.Value;

                Contract.Assume(source != null);
                Contract.Assume(target != null);

                // TODO: interfaces implemented by base classes?
                // ReSharper disable AssignNullToNotNullAttribute
                // ReSharper disable PossibleNullReferenceException
                var targetInterfaces = new HashSet<TypeReference>(target.GetAllInterfaces(), TypeReferenceEqualityComparer.Default);
                var sourceInterfaces = source.GetAllInterfaces();

                if (!sourceInterfaces.All(t => targetInterfaces.Contains(t)))
                    throw new WeavingException($@"{source} => {target} substitution error. Target must implement the same interfaces as source.", target);
                // ReSharper restore AssignNullToNotNullAttribute
                // ReSharper restore PossibleNullReferenceException

                var targetAndBases = target.GetSelfAndBaseTypes()
                    .Select((reference, index) => new { reference, index })
                    .ToDictionary(item => item.reference, item => item.index, TypeReferenceEqualityComparer.Default);

                var lastTargetIndex = 0;

                var sourceBase = source;

                while ((sourceBase = sourceBase.Resolve()?.BaseType) != null)
                {
                    Contract.Assume(sourceBase != null);

                    if (targetAndBases.TryGetValue(sourceBase, out var index))
                    {
                        if (index <= lastTargetIndex)
                            throw new WeavingException($@"{source} => {target} substitution error. There is a cross-mapping in the type hierarchies.", target);

                        lastTargetIndex = index;
                        continue;
                    }

                    if (substitutionMap.TryGetValue(sourceBase, out var targetBase) && targetAndBases.TryGetValue(targetBase, out index))
                    {
                        if (index < lastTargetIndex)
                            throw new WeavingException($@"{source} => {target} substitution error. There is a cross-mapping in the type hierarchies.", target);

                        lastTargetIndex = index;
                        continue;
                    }

                    invalidTypes[sourceBase] = new WeavingException(
$@"{source} => {target} substitution error. {source} derives from {sourceBase}, but there is no direct or substituted counterpart for {sourceBase} in the targets base classes.
Either derive {target} from {sourceBase}, or substitute {sourceBase} with {target} or one of it's base classes."
                        , target);
                }
            }

            return invalidTypes;
        }

        [CanBeNull]
        public static SequencePoint TryGetSequencePoint([CanBeNull] this TypeReference type)
        {
            var definition = type?.Resolve();

            var method = definition?.Methods?.FirstOrDefault();

            return method == null ? null : definition.Module?.SymbolReader?.Read(method)?.SequencePoints?.FirstOrDefault();
        }

        public static void CheckRecursions(this IDictionary<TypeReference, TypeDefinition> substitutionMap, HashSet<TypeReference> substitutes)
        {
            var recursion = substitutionMap.Keys.FirstOrDefault(item => substitutes.Contains(item));
            if (recursion != null)
                throw new WeavingException($"{recursion} is both source and target of a substitution.", recursion);
        }
    }
}
