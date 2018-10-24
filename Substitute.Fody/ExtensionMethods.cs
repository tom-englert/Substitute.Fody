using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Substitute
{
    internal static class ExtensionMethods
    {
        [NotNull]
        public static IReadOnlyDictionary<TypeReference, SubstitutionTarget> CreateSubstitutionMap([NotNull] this ICustomAttributeProvider attributeProvider, Parameters defaultParameters)
            => attributeProvider.CreateSubstitutionMap(defaultParameters, new Dictionary<TypeReference, SubstitutionTarget>(TypeReferenceEqualityComparer.Default));

        [NotNull]
        public static IReadOnlyDictionary<TypeReference, SubstitutionTarget> CreateSubstitutionMap([NotNull] this ICustomAttributeProvider attributeProvider, Parameters defaultParameters, [NotNull] IReadOnlyDictionary<TypeReference, SubstitutionTarget> parentDefinitions)
            => attributeProvider.GetTypeMappings(defaultParameters, parentDefinitions);

        [NotNull]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute"), SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private static IReadOnlyDictionary<TypeReference, SubstitutionTarget> GetTypeMappings([NotNull] this ICustomAttributeProvider attributeProvider, Parameters defaultParameters, [NotNull] IReadOnlyDictionary<TypeReference, SubstitutionTarget> parentDefinitions)
        {
            // Defer creation of the new dictionary until it is required.
            // In case there are no new mappings, we can use the instance that contains the parent attributes.
            Dictionary<TypeReference, SubstitutionTarget> newMappings = null;
            HashSet<TypeReference> duplicates = null;

            var customAttributes = attributeProvider.CustomAttributes
                .Where(ca => ca.AttributeType?.FullName == "Substitute.SubstituteAttribute")
                .Where(attr => attr.ConstructorArguments.Count == 2)
                .Where(attr => attr.ConstructorArguments.All(ca => ca.Type.FullName == "System.Type"));

            foreach (var customAttribute in customAttributes)
            {
                newMappings = newMappings ?? parentDefinitions.ToDictionary(TypeReferenceEqualityComparer.Default);
                duplicates = duplicates ?? new HashSet<TypeReference>(TypeReferenceEqualityComparer.Default);

                var sourceType = (TypeReference)customAttribute.ConstructorArguments[0].Value;
                var targetType = ((TypeReference)customAttribute.ConstructorArguments[1].Value).ResolveStrict();

                if (!duplicates.Add(sourceType))
                    throw new WeavingException($"Duplicate substitution mapping for type {sourceType}.", sourceType);

                var currentParameters = defaultParameters;
                var isDisable = false;
                foreach (var prop in customAttribute.Properties)
                {
                    if (prop.Name == "Disable")
                        isDisable = (bool)prop.Argument.Value;

                    if (prop.Name == "DoNotChangeSignature")
                        currentParameters._DoNotChangeSignature = (bool)prop.Argument.Value;

                    if (prop.Name == "KeepBaseMemberSignature")
                        currentParameters._KeepBaseMemberSignature = (bool)prop.Argument.Value;
                }

                if (isDisable)
                    newMappings.Remove(sourceType);
                else
                {
                    if ((newMappings ?? parentDefinitions).ContainsKey(sourceType))
                    {
                        var currentTarget = (newMappings ?? parentDefinitions)[sourceType];
                        var newTarget = new SubstitutionTarget(targetType, currentTarget.Parameters.Apply(currentParameters));
                        if (!currentTarget.Equals(newTarget))
                            newMappings[sourceType] = newTarget;
                    }
                    else
                        newMappings.Add(sourceType, new SubstitutionTarget(targetType, currentParameters));
                }
            }

            return newMappings ?? parentDefinitions;
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
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public static IEnumerable<TypeReference> GetAllInterfaces([NotNull] this TypeReference type)
        {
            return type.GetSelfAndBaseTypes().SelectMany(t => t.ResolveStrict().Interfaces.Select(i => i.InterfaceType));
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
        public static IDictionary<TypeReference, Exception> GetUnmappedTypeErrors([NotNull] this IReadOnlyDictionary<TypeReference, SubstitutionTarget> substitutionMap)
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
                var targetInterfaces = new HashSet<TypeReference>(target.TargetType.GetAllInterfaces(), TypeReferenceEqualityComparer.Default);
                var sourceInterfaces = source.GetAllInterfaces();

                if (!sourceInterfaces.All(t => targetInterfaces.Contains(t)))
                    throw new WeavingException($@"{source} => {target.TargetType} substitution error. Target must implement the same interfaces as source.", target.TargetType);
                // ReSharper restore AssignNullToNotNullAttribute
                // ReSharper restore PossibleNullReferenceException

                var targetAndBases = target.TargetType.GetSelfAndBaseTypes()
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
                            throw new WeavingException($@"{source} => {target.TargetType} substitution error. There is a cross-mapping in the type hierarchies.", target.TargetType);

                        lastTargetIndex = index;
                        continue;
                    }

                    if (substitutionMap.TryGetValue(sourceBase, out var targetBase) && targetAndBases.TryGetValue(targetBase.TargetType, out index))
                    {
                        if (index < lastTargetIndex)
                            throw new WeavingException($@"{source} => {target.TargetType} substitution error. There is a cross-mapping in the type hierarchies.", target.TargetType);

                        lastTargetIndex = index;
                        continue;
                    }

                    invalidTypes[sourceBase] = new WeavingException(
$@"{source} => {target.TargetType} substitution error. {source} derives from {sourceBase}, but there is no direct or substituted counterpart for {sourceBase} in the targets base classes.
Either derive {target.TargetType} from {sourceBase}, or substitute {sourceBase} with {target.TargetType} or one of it's base classes."
                        , target.TargetType);
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

        public static void CheckRecursions([NotNull] this IReadOnlyDictionary<TypeReference, SubstitutionTarget> substitutionMap, [NotNull, ItemNotNull] HashSet<TypeReference> substitutes)
        {
            var recursion = substitutionMap.Keys.FirstOrDefault(substitutes.Contains);

            if (recursion != null)
                throw new WeavingException($"{recursion} is both source and target of a substitution.", recursion);
        }

        [NotNull]
        public static TypeDefinition ResolveStrict([NotNull] this TypeReference reference)
        {
            return reference.Resolve() ?? throw new WeavingException($"Unable to resolve type {reference}", reference);
        }

        private static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>([NotNull] this IReadOnlyDictionary<TKey, TValue> source, IEqualityComparer<TKey> comparer)
        {
            var result = new Dictionary<TKey, TValue>(source.Count, comparer);

            foreach (var kvp in source)
                result.Add(kvp.Key, kvp.Value);

            return result;
        }
    }
}
