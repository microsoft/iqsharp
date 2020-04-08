// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;

using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Jupyter;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    ///     A magic command that lists what magic commands are currently
    ///     available.
    /// </summary>
    public class LsMagicMagic : AbstractMagic
    {
        private readonly IMagicSymbolResolver resolver;
        /// <summary>
        ///     Given a given snippets collection, constructs a new magic command
        ///     that queries callables defined in that snippets collection.
        /// </summary>
        public LsMagicMagic(IMagicSymbolResolver resolver) : base(
            "lsmagic",
            new Documentation
            {
                Summary = "Returns a list of all currently available magic commands."
            })
        {
            this.resolver = resolver;
        }

        /// <inheritdoc />
        public override ExecutionResult Run(string input, IChannel channel) =>
            // TODO: format as something nicer than a table.
            resolver
                .FindAllMagicSymbols()
                .Select(magic => new
                {
                    Name = magic.Name,
                    Documentation = magic.Documentation,
                    AssemblyName = magic.GetType().Assembly.GetName().Name
                })
                .OrderBy(magic => magic.Name)
                .ToList()
                .ToExecutionResult();
    }
}
