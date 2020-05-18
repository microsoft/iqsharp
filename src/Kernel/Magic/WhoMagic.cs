// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;

using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Kernel;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    ///     A magic command that can be used to query the functions and operations
    ///     defined within an IQ# session.
    /// </summary>
    public class WhoMagic : AbstractMagic
    {
        /// <summary>
        ///     Given a given snippets collection, constructs a new magic command
        ///     that queries callables defined in that snippets collection.
        /// </summary>
        public WhoMagic(ISnippets snippets) : base(
            "who",
            new Documentation
            {
                Summary = "Provides actions related to the current workspace."
            })
        {
            this.Snippets = snippets;
        }

        /// <summary>
        ///     The snippets collection queried by this magic command.
        /// </summary>
        public ISnippets Snippets { get; }

        /// <inheritdoc />
        public override ExecutionResult Run(string input, IChannel channel) =>
            Snippets.Operations
                .Select(op => op.FullName)
                .OrderBy(name => name)
                .ToArray()
                .ToExecutionResult();
    }
}
