// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;

using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Jupyter;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    public class WhoMagic : AbstractMagic
    {
        public WhoMagic(ISnippets snippets) : base(
            "who", 
            new Documentation {
                Summary = "Provides actions related to the current workspace."
            })
        {
            this.Snippets = snippets;
        }

        public ISnippets Snippets { get; }

        public override ExecutionResult Run(string input, IChannel channel) =>
            Snippets.Operations
                .Select(op => op.FullName)
                .OrderBy(name => name)
                .ToArray()
                .ToExecutionResult();
    }
}
