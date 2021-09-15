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

        private IEnumerable<String> Paths { get; }

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
            RoslynMetadatas = RoslynInit(Paths);
            QsMetadatas = QsInit(Paths);
        }

        private CompilerMetadata(IEnumerable<String> paths, IEnumerable<MetadataReference> roslyn, QsReferences qsharp)
        {
            Paths = paths;
            RoslynMetadatas = roslyn;
            QsMetadatas = qsharp;
        }

        /// <summary>
        /// Calculates the paths for all the Assemblies and their dependencies.
        /// </summary>
        private static List<string> PathsInit(IEnumerable<AssemblyInfo> assemblies, IEnumerable<string> seed = null)
        {
            var found = new List<string>(seed ?? Enumerable.Empty<string>());
            foreach (var a in assemblies)
            {
                AddReferencesPaths(found, a.Assembly, a.Location);
            }
            return found;
        }

        private static void AddReferencesPaths(List<string> found, Assembly asm, string location)
        {
            if (string.IsNullOrEmpty(location)) return;

            if (found.Contains(location))
            {
                return;
            }

            found.Add(location);

            foreach (var a in asm.GetReferencedAssemblies())
            {
                try
                {
                    var assm = Assembly.Load(a);
                    AddReferencesPaths(found, assm, assm.Location);
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
            new QsReferences(ProjectManager.LoadReferencedAssemblies(paths, ignoreDllResources: false));

        public CompilerMetadata WithAssemblies(params AssemblyInfo[] assemblies)
        {
            var extraPaths = PathsInit(assemblies, Paths);
            var extraRoslyn = RoslynInit(extraPaths);
            var extraQsharp = QsInit(Paths.Union(extraPaths));

            return new CompilerMetadata(extraPaths, extraRoslyn, extraQsharp);
        }
    }
}
