using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Substitute
{
    internal static class ExtensionMethods
    {
        public static IDictionary<TypeReference, TypeDefinition> CreateSubstitutionMap(this ModuleDefinition moduleDefinition)
            => moduleDefinition.GetTypeMappings()
                .VerfifyNoDuplicates()
                .ToDictionary(item => item.Key, item => item.Value, TypeReferenceEqualityComparer.Default);

        private static IList<KeyValuePair<TypeReference, TypeDefinition>> VerfifyNoDuplicates(this IList<KeyValuePair<TypeReference, TypeDefinition>> typeMappings)
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

        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute"), SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private static IList<KeyValuePair<TypeReference, TypeDefinition>> GetTypeMappings(this ModuleDefinition moduleDefinition)
        {
            return moduleDefinition.Assembly.CustomAttributes
                .Where(ca => ca.AttributeType?.FullName == "Substitute.SubstituteAttribute")
                .Where(attr => attr.ConstructorArguments.Count == 2)
                .Where(attr => attr.ConstructorArguments.All(ca => ca.Type.FullName == "System.Type"))
                .Select(attr => new KeyValuePair<TypeReference, TypeDefinition>((TypeReference)attr.ConstructorArguments[0].Value, moduleDefinition.ImportReference((TypeReference)attr.ConstructorArguments[1].Value).ResolveStrict()))
                .ToArray();
        }


        public static IEnumerable<TypeReference> GetBaseTypes(this TypeReference type)
        {
            TypeReference? item = type;

            while ((item = item?.Resolve()?.BaseType) != null)
            {
                yield return item;
            }
        }

        public static IEnumerable<TypeReference> GetSelfAndBaseTypes(this TypeReference type)
        {
            yield return type;

            TypeReference? item = type;

            while ((item = item.Resolve()?.BaseType) != null)
            {
                yield return item;
            }
        }

        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public static IEnumerable<TypeReference> GetAllInterfaces(this TypeReference type)
        {
            return type.GetSelfAndBaseTypes().SelectMany(t => t.ResolveStrict().Interfaces.Select(i => i.InterfaceType));
        }


        public static void ReplaceItems<T>(this IList<T?> items, Func<T?, T?> replace)
            where T : TypeReference
        {
            for (var i = 0; i < items.Count; i++)
            {
                items[i] = replace(items[i]);
            }
        }

        public static string? GetSignature(this IMemberDefinition? member, TypeDefinition? targetType = null)
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

        public static IDictionary<TypeReference, Exception> GetUnmappedTypeErrors(this IDictionary<TypeReference, TypeDefinition> substitutionMap)
        {
            var invalidTypes = new Dictionary<TypeReference, Exception>(TypeReferenceEqualityComparer.Default);

            foreach (var entry in substitutionMap)
            {
                var source = entry.Key;
                var target = entry.Value;

                // TODO: interfaces implemented by base classes?
                var targetInterfaces = new HashSet<TypeReference>(target.GetAllInterfaces(), TypeReferenceEqualityComparer.Default);
                var sourceInterfaces = source.GetAllInterfaces();

                if (!sourceInterfaces.All(t => targetInterfaces.Contains(t)))
                    throw new WeavingException($@"{source} => {target} substitution error. Target must implement the same interfaces as source.", target);

                var targetAndBases = target.GetSelfAndBaseTypes()
                    .Select((reference, index) => new { reference, index })
                    .ToDictionary(item => item.reference, item => item.index, TypeReferenceEqualityComparer.Default);

                var lastTargetIndex = 0;

                TypeReference? sourceBase = source;

                while ((sourceBase = sourceBase.Resolve()?.BaseType) != null)
                {
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

        public static SequencePoint? TryGetSequencePoint(this TypeReference? type)
        {
            var definition = type?.Resolve();

            var method = definition?.Methods?.FirstOrDefault();

            return method == null ? null : definition!.Module?.SymbolReader?.Read(method)?.SequencePoints?.FirstOrDefault();
        }

        public static void CheckRecursions(this IDictionary<TypeReference, TypeDefinition> substitutionMap, HashSet<TypeReference> substitutes)
        {
            var recursion = substitutionMap.Keys.FirstOrDefault(substitutes.Contains);

            if (recursion != null)
                throw new WeavingException($"{recursion} is both source and target of a substitution.", recursion);
        }

        public static TypeDefinition ResolveStrict(this TypeReference reference)
        {
            return reference.Resolve() ?? throw new WeavingException($"Unable to resolve type {reference}", reference);
        }
    }
}
