// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler;
using System.IO;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// This class stores the information about a .net core assembly.
    /// If the assembly was built from Q# code, then it also keeps track of the list of Operations
    /// defined in it, plus the corresponding SyntaxTree.
    /// </summary>
    public class AssemblyInfo : IEquatable<AssemblyInfo>
    {
        /// List of operations found. Calculated on demand.
        private Lazy<OperationInfo[]> _operations;

        /// <summary>
        /// Constructor for non Q# compiled assemblies.
        /// </summary>
        public AssemblyInfo(Assembly assembly) : this(assembly, location: null, syntaxTree: null, qirBitcode: null)
        {
        }

        /// <summary>
        /// Constructor for Q# compiled assemblies.
        /// </summary>
        public AssemblyInfo(Assembly assembly, string location, QsNamespace[] syntaxTree, Stream qirBitcode)
        {
            Assembly = assembly;
            Location = location ?? assembly?.Location;
            SyntaxTree = syntaxTree;
            QirBitcode = qirBitcode;
            _operations = new Lazy<OperationInfo[]>(InitOperations);
        }

        /// <summary>
        /// The actual Assembly we're wrapping.
        /// </summary>
        public Assembly Assembly { get; }

        /// <summary>
        /// The path (location) in disk of this assembly.
        /// </summary>
        public string Location { get; }

        /// <summary>
        /// For Q#-based assemblies, the corresponding SyntaxTree.
        /// </summary>
        public QsNamespace[] SyntaxTree { get; }

        public Stream QirBitcode { get; }

        /// <summary>
        /// For Q#-based assemblies, the corresponding operations found in the SyntaxTree.
        /// </summary>
        public IEnumerable<OperationInfo> Operations => _operations.Value;

        /// <summary>
        ///  Used to lazily calculate operations in an assembly.
        ///  Assumes that all Types in the Assembly are for operations.
        /// </summary>
        private OperationInfo[] InitOperations()
        {
            if (Assembly == null) return new OperationInfo[0];

            // Parse the assembly headers to find which types are operation or function types.
            var logger = new QSharpLogger(null); 
            var refs = ProjectManager.LoadReferencedAssemblies(new[] { Location }, d => logger.Log(d), ex => logger.Log(ex), ignoreDllResources:CompilerMetadata.LoadFromCsharp);

            var callables = refs.SelectMany(pair => pair.Value.Callables);

            var ops = new List<OperationInfo>();
            foreach (var callable in callables)
            {
                // Find the associated type.
                var fullName = callable.QualifiedName.ToFullName();
                var type = Assembly.GetType(fullName);
                var info = new OperationInfo(type, callable);
                ops.Add(info);
            }

            // Makes sure Deprecated operations are pushed to the bottom so that they are resolved second:
            return ops
                .OrderBy(op => op.Header.Attributes.Any(BuiltIn.MarksDeprecation) ? 1 : 0)
                .ToArray();
        }

        #region Equals
        public override string ToString() => Assembly?.ToString();

        public override bool Equals(object obj) => Equals(obj as AssemblyInfo);

        public bool Equals(AssemblyInfo other) => Assembly?.FullName == other?.Assembly?.FullName;

        public override int GetHashCode() => Assembly?.FullName?.GetHashCode() ?? 0;

        public static bool operator ==(AssemblyInfo info1, AssemblyInfo info2) => info1?.Assembly?.FullName == info2?.Assembly?.FullName;

        public static bool operator !=(AssemblyInfo info1, AssemblyInfo info2) => !(info1 == info2);
        #endregion

        public static AssemblyInfo Create(Assembly assembly) => assembly == null ? null : new AssemblyInfo(assembly);

    }
}
