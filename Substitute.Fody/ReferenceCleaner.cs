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

        public static void RemoveReferences([NotNull] this ModuleDefinition moduleDefinition, [NotNull] ILogger logger)
        {
            ProcessAssembly(moduleDefinition);

            // ReSharper disable once AssignNullToNotNullAttribute
            // ReSharper disable once PossibleNullReferenceException
            var referenceToRemove = moduleDefinition.AssemblyReferences.FirstOrDefault(x => x.Name == "Substitute");
            if (referenceToRemove == null)
            {
                logger.LogInfo("\tNo reference to 'Substitute' found. References not modified.");
                return;
            }

            logger.LogInfo("\tRemoving reference to 'Substitute'.");
            if (!moduleDefinition.AssemblyReferences.Remove(referenceToRemove))
            {
                logger.LogWarning("\tCould not remove all references to 'Substitute'.");
            }
        }
    }
}