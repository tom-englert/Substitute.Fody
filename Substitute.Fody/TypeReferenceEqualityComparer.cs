using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using Mono.Cecil;

namespace Substitute
{
    internal sealed class TypeReferenceEqualityComparer : IEqualityComparer<TypeReference>
    {
        private TypeReferenceEqualityComparer()
        {
        }

        [NotNull]
        internal static IEqualityComparer<TypeReference> Default { get; } = new TypeReferenceEqualityComparer();

        internal static bool Equals([CanBeNull] TypeReference x, [CanBeNull] TypeReference y) => Default.Equals(x, y);

        internal static int GetHashCode([CanBeNull] TypeReference obj) => Default.GetHashCode(obj);

        bool IEqualityComparer<TypeReference>.Equals(TypeReference x, TypeReference y) => GetKey(x) == GetKey(y);

        int IEqualityComparer<TypeReference>.GetHashCode([CanBeNull] TypeReference obj) => GetKey(obj)?.GetHashCode() ?? 0;

        [CanBeNull]
        private static string GetKey([CanBeNull] TypeReference obj)
        {
            if (obj == null)
                return null;

            return GetAssemblyName(obj.Scope) + "|" + obj.FullName;
        }

        [CanBeNull]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute"), SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private static string GetAssemblyName([CanBeNull] IMetadataScope scope)
        {
            if (scope == null)
                return null;

            if (scope is ModuleDefinition md)
            {
                return md.Assembly.FullName;
            }

            return scope.ToString();
        }
    }
}