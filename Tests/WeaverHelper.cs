using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

using JetBrains.Annotations;

using Mono.Cecil;

using NUnit.Framework;

using Substitute;

using TomsToolbox.Core;

namespace Tests
{
    internal class WeaverHelper
    {
        [NotNull]
        private static readonly Dictionary<string, WeaverHelper> _cache = new Dictionary<string, WeaverHelper>();

        [NotNull]
        public Assembly Assembly { get; }
        [NotNull]
        public string NewAssemblyPath { get; }
        [NotNull]
        public string OriginalAssemblyPath { get; }

#if (!DEBUG)
        private const string Configuration = "Release";
#else
        private const string Configuration = "Debug";
#endif

        [NotNull]
        public static WeaverHelper Create([NotNull] string assemblyKey = "AssemblyToProcess")
        {
            lock (typeof(WeaverHelper))
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                return _cache.ForceValue(assemblyKey, _ => new WeaverHelper(assemblyKey));
            }
        }

        [NotNull, ItemNotNull]
        public IList<string> Errors { get; } = new List<string>();

        private WeaverHelper([NotNull] string assemblyKey)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            // ReSharper disable once PossibleNullReferenceException
            var assemblyName = Path.GetFileNameWithoutExtension(assemblyKey);

            var projectDir = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, $@"..\..\..\{assemblyName}"));

            OriginalAssemblyPath = Path.Combine(projectDir, $@"bin\{Configuration}", $@"{assemblyKey}.dll");
            NewAssemblyPath = OriginalAssemblyPath.Replace(".dll", "2.dll");

            using (var moduleDefinition = ModuleDefinition.ReadModule(OriginalAssemblyPath, new ReaderParameters(ReadingMode.Immediate) { ReadSymbols = true }))
            {
                Debug.Assert(moduleDefinition != null, "moduleDefinition != null");

                var weavingTask = new ModuleWeaver
                {
                    ModuleDefinition = moduleDefinition
                };

                weavingTask.LogError = s => Errors.Add(s);
                weavingTask.LogErrorPoint = (s, point) => Errors.Add(s);
                weavingTask.Execute();

                var assemblyNameDefinition = moduleDefinition.Assembly?.Name;
                Debug.Assert(assemblyNameDefinition != null, "assemblyNameDefinition != null");

                // ReSharper disable once PossibleNullReferenceException
                assemblyNameDefinition.Version = new Version(0, 2, 0, assemblyNameDefinition.Version.Revision);
                moduleDefinition.Write(NewAssemblyPath, new WriterParameters { WriteSymbols = true });
            }

            // ReSharper disable once AssignNullToNotNullAttribute
            Assembly = Assembly.LoadFile(NewAssemblyPath);
        }
    }
}
