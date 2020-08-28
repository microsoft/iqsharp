// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;

using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Jupyter;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    ///      Represents information about a single magic symbol returned
    ///      by the <c>%lsmagic</c> magic command.
    /// </summary>
    internal struct MagicSymbolSummary
    {
        public string Name;
        public Documentation Documentation;
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
        public LsMagicMagic(IMagicSymbolResolver resolver, IExecutionEngine engine) : base(
            "lsmagic",
            new Microsoft.Jupyter.Core.Documentation
            {
                Summary = "Returns a list of all currently available magic commands.",
                Description = @"
                    This magic command lists all of the magic commands available in the IQ# kernel,
                    as well as those defined in any packages that have been loaded in the current
                    session via the [`%package` magic command](https://docs.microsoft.com/qsharp/api/iqsharp-magic/package).
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
            })
        {
            this.resolver = resolver;
            (engine as IQSharpEngine).RegisterDisplayEncoder(new MagicSymbolSummariesToHtmlEncoder());
            (engine as IQSharpEngine).RegisterDisplayEncoder(new MagicSymbolSummariesToTextEncoder());
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
