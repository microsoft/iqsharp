// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Jupyter;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    ///     A magic command that lists any open namespaces and their aliases.
    /// </summary>
    public class LsOpenMagic : AbstractMagic
    {
        /// <summary>
        ///     Constructs an instance of %lsopen given an instance of the
        ///     compiler service.
        /// </summary>
        public LsOpenMagic(IExecutionEngine engine, ILogger<LsOpenMagic> logger) : base(
            "lsopen",
            new Microsoft.Jupyter.Core.Documentation
            {
                Summary = "Lists currently opened namespaces and their aliases.",
                Description = @"
                    This magic command lists any namespaces that have been made
                    available using `open` statements, along with any aliases
                    that may have been assigned to those namespaces.
                ".Dedent(),
                Examples = new []
                {
                    @"
                        Print a list of all currently opened namespaces:
                        ```
                        In []: %lsopen
                        Out[]: Namespace                     Alias
                               ----------------------------- ----
                               Microsoft.Quantum.Canon
                               Microsoft.Quantum.Diagnostics Diag
                               Microsoft.Quantum.Intrinsic
                        ```
                    ".Dedent()
                }
            }, logger)
        {
            this.Engine = (IQSharpEngine)engine;
        }

        /// <summary>
        ///     The engine used to identify open namespaces.
        /// </summary>
        public IQSharpEngine Engine { get; }


        /// <inheritdoc />
        public override ExecutionResult Run(string? input, IChannel channel) => new Table<(string, string?)>
        {
            Columns = new List<(string, Func<(string, string?), string>)>
            {
                ("Namespace", item => item.Item1),
                ("Alias", item => item.Item2 ?? "")
            },
            Rows = Engine.GloballyOpenNamespaces
                .OrderBy(item => item.Namespace)
                .Select(
                    item => (item.Namespace, item.Alias)
                )
                .ToList()
        }.ToExecutionResult();
    }
}
