using JetBrains.Annotations;
using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace Substitute
{
    internal sealed class SubstitutionTarget : IEquatable<SubstitutionTarget>
    {
        [NotNull]
        internal TypeDefinition TargetType { get; }

        [NotNull]
        internal Parameters Parameters { get; }

        internal SubstitutionTarget([NotNull] TypeDefinition targetType, Parameters parameters)
        {
            TargetType = targetType;
            Parameters = parameters;
        }

        public override bool Equals(object obj) => Equals(obj as SubstitutionTarget);

        public bool Equals(SubstitutionTarget other)
        {
            if (other == null) return false;

            return TypeReferenceEqualityComparer.Equals(TargetType, other.TargetType) && Parameters.Equals(other.Parameters);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return TypeReferenceEqualityComparer.GetHashCode(TargetType) * 23 + Parameters.GetHashCode();
            }
        }

        public static bool operator ==(SubstitutionTarget target1, SubstitutionTarget target2)
        {
            return EqualityComparer<SubstitutionTarget>.Default.Equals(target1, target2);
        }

        public static bool operator !=(SubstitutionTarget target1, SubstitutionTarget target2)
        {
            return !(target1 == target2);
        }
    }
}
