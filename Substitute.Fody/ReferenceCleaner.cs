using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using Mono.Cecil;

namespace Substitute
{
    internal static class ReferenceCleaner
    {
        [NotNull]
        private static readonly HashSet<string> _attributeNames = new HashSet<string>
        {
            "Substitute.SubstituteAttribute"
        };

        private static void ProcessAssembly([NotNull] ModuleDefinition moduleDefinition)
        {
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once AssignNullToNotNullAttribute
            RemoveAttributes(moduleDefinition.Assembly.CustomAttributes);
        }

        private static void RemoveAttributes([NotNull, ItemNotNull] ICollection<CustomAttribute> customAttributes)
        {
            var attributes = customAttributes
                .Where(attribute => _attributeNames.Contains(attribute.Constructor?.DeclaringType?.FullName))
                .ToArray();

            foreach (var customAttribute in attributes.ToList())
            {
                customAttributes.Remove(customAttribute);
            }
        }
    }
}