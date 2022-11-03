// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    ///      Represents information about a single magic symbol returned
    ///      by the <c>%lsmagic</c> magic command.
    /// </summary>
    internal struct MagicSymbolSummary
    {
        public string Name;
        public Microsoft.Jupyter.Core.Documentation Documentation;
        public string AssemblyName;
    }

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
        public LsMagicMagic(IMagicSymbolResolver resolver, IExecutionEngine engine, ILogger<LsMagicMagic> logger) : base(
            "lsmagic",
            new Microsoft.Jupyter.Core.Documentation
            {
                Summary = "Returns a list of all currently available magic commands.",
                Description = $@"
                    This magic command lists all of the magic commands available in the IQ# kernel,
                    as well as those defined in any packages that have been loaded in the current
                    session via the [`%package` magic command]({KnownUris.ReferenceForMagicCommand("package")}).
                ".Dedent(),
                Examples = new []
                {
                    @"
                        Display the list of available magic commands:
                        ```
                        In []: %lsmagic
                        Out[]: <detailed list of all available magic commands>
                        ```
                    ".Dedent(),
                }
            }, logger)
        {
            this.resolver = resolver;
            if (engine is BaseEngine baseEngine)
            {
                baseEngine.RegisterDisplayEncoder(new MagicSymbolSummariesToHtmlEncoder());
                baseEngine.RegisterDisplayEncoder(new MagicSymbolSummariesToTextEncoder());
            }
            else
            {
                throw new Exception($"Expected execution engine to be an IQ# engine, but was {engine.GetType()}.");
            }
        }

        /// <inheritdoc />
        public override ExecutionResult Run(string input, IChannel channel) =>
            resolver
                .FindAllMagicSymbols()
                .Select(magic => new MagicSymbolSummary
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
