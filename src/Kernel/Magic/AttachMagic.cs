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
                Summary = "Attaches a debugger to the current IQ# session.",
                Description = @"
                    If no debugger is attached to the current IQ# session, launches a new debugger and attaches it, allowing for stepping through IQ# implementation code.

                    If a debugger is already attached, this magic command acts as a breakpoint when executed.

                    > **NOTE:** This command is not included in release versions of the IQ# kernel, and is only intended for use by IQ# contributors.
                    > If you are interested in debugging Q# code written in IQ#, please see the [`%debug` magic command](https://docs.microsoft.com/qsharp/api/iqsharp-magic/debug) instead.
                ".Dedent(),
                Examples = new string []
                {
                    @"
                        Attach a debugger to the current IQ# session:
                        ```
                        %attach
                        ```
                    ".Dedent()
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
