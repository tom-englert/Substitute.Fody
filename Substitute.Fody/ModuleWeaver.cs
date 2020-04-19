// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

using System.Collections.Generic;

using FodyTools;

namespace Substitute
{
    public class ModuleWeaver : AbstractModuleWeaver
    {
        public override void Execute()
        {
            ModuleDefinition.Weave(this);
            ModuleDefinition.RemoveReferences();
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield break;
        }

        public override bool ShouldCleanReference => true;
    }
}