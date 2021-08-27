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
    public class ShowMagic : AbstractMagic
    {
        private ISnippets Snippets;

        public ShowMagic(ISnippets snippets, ILogger<ShowMagic> logger) : base(
            "show",
            new Microsoft.Jupyter.Core.Documentation
            {
                Summary = "Shows the source code for a function, operation, or user-defined type.",
                Description = @"TODO".Dedent(),
                Examples = new string []
                {
                }
            }, logger)
        {
            this.Snippets = snippets;
        }


        /// <inheritdoc />
        public override ExecutionResult Run(string? input, IChannel channel)
        {
            // TODO: look for definitions in the workspace, too!
            input = input?.Trim() ?? "";
            var expandedInput = 
                !input.Contains(".")
                ? $"{Microsoft.Quantum.IQSharp.Snippets.SNIPPETS_NAMESPACE}.{input}"
                : input;
            if (Snippets.Declarations.TryGetValue(expandedInput, out var declaration))
            {
                return declaration.ToExecutionResult();
            }
            else
            {
                channel.Stderr($"No operation, function, or user-defined type with name '{input}' has been defined.");
                return ExecuteStatus.Error.ToExecutionResult();
            }
        }
    }
}
