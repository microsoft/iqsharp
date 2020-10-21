// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;

using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Jupyter;

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
            new Microsoft.Jupyter.Core.Documentation
            {
                Summary = "Lists the Q# operations available in the current session.",
                Description = @"
                    This magic command returns a list of Q# operations and functions that are available
                    in the current IQ# session for use with magic commands such as `%simulate`
                    and `%estimate`.

                    The list will include Q# operations and functions which have been defined interactively
                    within cells in the current notebook (after the cells have been executed),
                    as well as any Q# operations and functions defined within .qs files in the current folder.
                ".Dedent(),
                Examples = new []
                {
                    @"
                        Display the list of Q# operations and functions available in the current session:
                        ```
                        In []: %who
                        Out[]: <list of Q# operation and function names>
                        ```
                    ".Dedent(),
                }
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
