// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.IQSharp.Common;

using QsReferences = Microsoft.Quantum.QsCompiler.CompilationBuilder.References;

namespace Microsoft.Quantum.IQSharp
{
    public class CompilerMetadata
    {
        internal static readonly bool LoadFromCsharp = false; // todo: we should make this properly configurable

        private static readonly ImmutableHashSet<string> KnownClassicalAssemblies =
            new string[]
            {
                "netstandard",
                "NumSharp.Core",
                "Newtonsoft.Json"
            }
            .ToImmutableHashSet();

        private Dictionary<String, bool> Paths { get; }

        /// <summary>
        /// The list of Assemblies and their dependencies in the format the C# compiler (Roslyn) expects them.
        /// </summary>
        public IEnumerable<MetadataReference> RoslynMetadatas { get; }

        /// <summary>
        ///  The list of Assemblies and their dependencies in the format the Q# compiler expects them.
        /// </summary>
        public QsReferences QsMetadatas { get; }

        public CompilerMetadata(IEnumerable<AssemblyInfo> assemblies)
        {
            Paths = PathsInit(assemblies);
            RoslynMetadatas = RoslynInit(Paths.Keys);
            QsMetadatas = QsInit(Paths.Where((item) => item.Value).Select(item => item.Key));
        }

        private CompilerMetadata(Dictionary<string, bool> paths, IEnumerable<MetadataReference> roslyn, QsReferences qsharp)
        {
            Paths = paths;
            RoslynMetadatas = roslyn;
            QsMetadatas = qsharp;
        }

        /// <summary>
        /// Calculates the paths for all the Assemblies and their dependencies.
        /// </summary>
        private static Dictionary<string, bool> PathsInit(IEnumerable<AssemblyInfo> assemblies, Dictionary<string, bool> seed = null)
        {
            var found = new Dictionary<string, bool>(seed ?? new Dictionary<string, bool>());
            foreach (var a in assemblies)
            {
                AddReferencesPaths(found, a.Assembly, a.Location, !KnownClassicalAssemblies.Contains(a.Assembly.GetName().Name));
            }
            return found;
        }

        private static void AddReferencesPaths(Dictionary<string, bool> found, Assembly asm, string location, bool isPossiblyQuantum)
        {
            if (string.IsNullOrEmpty(location)) return;

            if (found.ContainsKey(location))
            {
                return;
            }

            found.Add(location, isPossiblyQuantum);

            foreach (var a in asm.GetReferencedAssemblies())
            {
                try
                {
                    var assm = Assembly.Load(a);
                    AddReferencesPaths(found, assm, assm.Location, isPossiblyQuantum && !KnownClassicalAssemblies.Contains(assm.GetName().Name));
                }
                catch (Exception)
                {
                    //Ignore assembly if it can't be loaded.
                }
            }
        }

        /// <summary>
        /// Calculates Roslyn's MetadataReference for all the Assemblies and their dependencies.
        /// </summary>
        private static ImmutableArray<MetadataReference> RoslynInit(IEnumerable<string> paths)
        {
            var mds = paths.Select(p => MetadataReference.CreateFromFile(p));
            return mds.Select(a => a as MetadataReference).ToImmutableArray();
        }

        /// <summary>
        /// Calculates Q# metadata needed for all the Assemblies and their dependencies.
        /// </summary>
        private static QsReferences QsInit(IEnumerable<string> paths) =>
            new QsReferences(ProjectManager.LoadReferencedAssemblies(
                paths, ignoreDllResources: false
            ));

        public CompilerMetadata WithAssemblies(params AssemblyInfo[] assemblies)
        {
            var extraPaths = PathsInit(assemblies, Paths);
            var extraRoslyn = RoslynInit(extraPaths.Keys);
            var extraQsharp = QsInit(Paths.Union(extraPaths).Where((item) => item.Value).Select(item => item.Key));

            return new CompilerMetadata(extraPaths, extraRoslyn, extraQsharp);
        }
    }
}
