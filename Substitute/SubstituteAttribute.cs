using System;
// ReSharper disable UnusedParameter.Local

namespace Substitute
{
    /// <summary>
    /// Add this attribute to your assembly to substitute one type with another, i.e. all usages of the original type will be replaced by the new type.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Assembly |
        AttributeTargets.Module |
        AttributeTargets.Class |
        AttributeTargets.Interface |
        AttributeTargets.Struct |
        AttributeTargets.Event |
        AttributeTargets.Field |
        AttributeTargets.Property |
        AttributeTargets.Method, AllowMultiple = true)]
    public sealed class SubstituteAttribute : Attribute
    {
        /// <summary>
        /// If this property is set to true, the specified substitution will be removed from the substitution list.
        /// </summary>
        /// <remarks>
        /// In case a substitution is specified that was never enabled, the attribute will not do anything.
        /// </remarks>
        public bool Disable { get; set; } = false;

        /// <summary>
        /// If this option is enabled, the substitution will only be applied to the method bodies,
        /// but not to any signature.
        /// </summary>
        public bool DoNotChangeSignature { get; set; }

        /// <summary>
        /// If this option is enabled, the signatures of a member overwriting another member that is not subject to the
        /// substitution, will be kept.
        /// </summary>
        public bool KeepBaseMemberSignature { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubstituteAttribute"/> class.
        /// </summary>
        /// <param name="original">The original that will be substituted.</param>
        /// <param name="substituteWith">The type that will substitute the original.</param>
        public SubstituteAttribute(Type original, Type substituteWith)
        {
        }
    }
}
