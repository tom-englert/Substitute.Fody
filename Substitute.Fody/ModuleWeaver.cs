// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

using System.Collections.Generic;
using Fody;
using JetBrains.Annotations;

using Mono.Cecil.Cil;

namespace Substitute
{
    public class ModuleWeaver : BaseModuleWeaver, ILogger
    {
        public override void Execute()
        {
            ModuleDefinition.Weave(this);
            ModuleDefinition.RemoveReferences(this);
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield break;
        }

        public override bool ShouldCleanReference => true;

        void ILogger.LogDebug([NotNull] string message)
        {
            LogDebug(message);
        }

        void ILogger.LogInfo([NotNull] string message)
        {
            LogInfo(message);
        }

        void ILogger.LogWarning([NotNull] string message)
        {
            LogWarning(message);
        }

        void ILogger.LogError([NotNull] string message, [CanBeNull] SequencePoint sequencePoint)
        {
            if (sequencePoint != null)
            {
                LogErrorPoint(message, sequencePoint);
            }
            else
            {
                LogError(message);
            }
        }
    }
}