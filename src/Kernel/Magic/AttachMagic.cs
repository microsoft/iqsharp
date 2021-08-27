// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Jupyter;

#if DEBUG

namespace Microsoft.Quantum.IQSharp.Kernel
{
    public class AttachMagic : AbstractMagic
    {
        private ISnippets Snippets;

        public AttachMagic(ILogger<AttachMagic> logger) : base(
            "attach",
            new Microsoft.Jupyter.Core.Documentation
            {
                Summary = "TODO",
                Description = @"TODO".Dedent(),
                Examples = new string []
                {
                }
            }, logger)
        { }


        /// <inheritdoc />
        public override ExecutionResult Run(string? input, IChannel channel)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }
            else
            {
                System.Diagnostics.Debugger.Launch();
            }

            return ExecuteStatus.Ok.ToExecutionResult();
        }
    }
}

#endif
