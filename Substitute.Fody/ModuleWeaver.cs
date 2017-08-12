// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

using System;

using JetBrains.Annotations;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Substitute
{
    public class ModuleWeaver : ILogger
    {
        // Will log an informational message to MSBuild
        public Action<string> LogDebug { get; set; }
        public Action<string> LogInfo { get; set; }
        public Action<string> LogWarning { get; set; }
        public Action<string> LogError { get; set; }
        public Action<string, SequencePoint> LogErrorPoint { get; set; }
        // An instance of Mono.Cecil.ModuleDefinition for processing
        public ModuleDefinition ModuleDefinition { get; set; }

        public ModuleWeaver()
        {
            LogDebug = LogInfo = LogWarning = LogError = _ => { };
            LogErrorPoint = (_, __) => { };
        }

        public void Execute()
        {
            ModuleDefinition.Weave(this);
            ModuleDefinition.RemoveReferences(this);
        }

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