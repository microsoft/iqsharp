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
using System.Collections.Concurrent;

namespace Microsoft.Quantum.IQSharp
{
    public class CompilerMetadata
    {
        internal static readonly bool LoadFromCsharp = false; // todo: we should make this properly configurable

        private IEnumerable<String> Paths { get; init; }

        /// <summary>
        /// The list of Assemblies and their dependencies in the format the C# compiler (Roslyn) expects them.
        /// </summary>
        public IEnumerable<MetadataReference> RoslynMetadatas { get; init; }

        /// <summary>
        ///  The list of Assemblies and their dependencies in the format the Q# compiler expects them.
        /// </summary>
        public QsReferences QsMetadatas { get; init; }

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

        private static readonly IDictionary<string, MetadataReference> metadataReferenceCache
            = new ConcurrentDictionary<string, MetadataReference>();

        /// <summary>
        /// Calculates Roslyn's MetadataReference for all the Assemblies and their dependencies.
        /// </summary>
        private static ImmutableArray<MetadataReference> RoslynInit(IEnumerable<string> paths) =>
            paths.Select(p =>
            {
                if (metadataReferenceCache.TryGetValue(p, out var cached))
                {
                    return cached;
                }
                else
                {
                    var mdRef = MetadataReference.CreateFromFile(p);
                    metadataReferenceCache[p] = mdRef;
                    return mdRef;
                }
            })
            .ToImmutableArray();

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
