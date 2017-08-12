using System;
// ReSharper disable UnusedParameter.Local

namespace Substitute
{
    /// <summary>
    /// Add this attribute to your assembly to substitute one type with another, i.e. all usages of the original type will be replaced by the new type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class SubstituteAttribute : Attribute
    {
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
