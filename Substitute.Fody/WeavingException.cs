using System;
using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using Mono.Cecil;

namespace Substitute
{
    // This exception is not designed to leave this assembly uncaught.
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic")]
    internal class WeavingException : Exception
    {
        public WeavingException([NotNull] string message, [CanBeNull] TypeReference type) : base(message)
        {
            Type = type;
        }

        [CanBeNull]
        public TypeReference Type { get; }
    }
}
