using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;

namespace Substitute
{
    internal static class ReferenceCleaner
    {
        private static readonly HashSet<string> _attributeNames = new HashSet<string>
        {
            "Substitute.SubstituteAttribute"
        };

        private static void ProcessAssembly(ModuleDefinition moduleDefinition)
        {
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once AssignNullToNotNullAttribute
            RemoveAttributes(moduleDefinition.Assembly.CustomAttributes);
        }

        private static void RemoveAttributes(ICollection<CustomAttribute> customAttributes)
        {
            var attributes = customAttributes
                .Where(attribute => _attributeNames.Contains(attribute.Constructor?.DeclaringType?.FullName ?? string.Empty))
                .ToArray();

            foreach (var customAttribute in attributes.ToList())
            {
                customAttributes.Remove(customAttribute);
            }
        }

        public static void RemoveReferences(this ModuleDefinition moduleDefinition)
        {
            ProcessAssembly(moduleDefinition);
        }
    }
}