// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.IQSharp.Kernel;
using Microsoft.Quantum.IQSharp.ExecutionPathTracer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Microsoft.Quantum.Experimental;
using Tests.IQSharp;


#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Microsoft.Quantum.Tests.IQSharp
{
    [TestClass]
    public class CompilerMetadataTests
    {
        [TestMethod]
        public async Task CoreIntrinsicsAreNotFilteredOut()
        {
            var metadata = new CompilerMetadata(References.QUANTUM_CORE_ASSEMBLIES);
            Assert.That.Enumerable(metadata.QsMetadatas.Declarations)
                .Any(decl =>
                    decl.Value.Callables.Any(callable =>
                        callable.QualifiedName.Namespace == "Microsoft.Quantum.Intrinsic" &&
                        callable.QualifiedName.Name == "R1Frac"
                    )
                );
        }

        [TestMethod]
        public async Task RoslynAssembliesIncludeClassicalReferences()
        {
            var metadata = new CompilerMetadata(References.QUANTUM_CORE_ASSEMBLIES);
            Assert.That.Enumerable(metadata.RoslynMetadatas.Select(o => o.Display))
                .Any(reference =>
                    reference?.Contains("System.Runtime.dll")
                    ?? false
                );
        }
    }

}
