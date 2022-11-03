// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Jupyter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    ///     A magic command that reports various performance metrics to the
    ///     user.
    /// </summary>
    public class PerformanceMagic : AbstractMagic
    {
        /// <summary>
        ///     Constructs a new performance command.
        /// </summary>
        public PerformanceMagic(ILogger<PerformanceMagic> logger) : base(
            "performance",
            new Microsoft.Jupyter.Core.Documentation {
                Summary = "Reports current performance metrics for this kernel.",
                Description = @"
                    Reports various performance metrics for the current IQ# kernel process, including:

                    - Managed RAM usage
                    - Total RAM usage
                    - Virtual memory size
                    - User time
                    - Total time
                ".Dedent(),
                Examples = new []
                {
                    @"
                        Display performance metrics for the current IQ# kernel process:
                        ```
                        In []: %performance
                        Out[]: Metric                        Value
                               ----------------------------  -------------
                               Managed RAM usage (bytes)     4.985 MiB
                               Total RAM usage (bytes)       54.543 MiB
                               Virtual memory size (bytes)   2.005 TiB
                               User time                     00:00:01.109
                               Total time                    00:00:01.437
                        ```
                    ".Dedent(),
                }
            }, logger)
        {
        }

    /// <inheritdoc />
        public override ExecutionResult Run(string? input, IChannel channel)
        {
            var currentProcess = Process.GetCurrentProcess();
            var performanceResult = new List<(string, string)>
            {
                ("Managed RAM usage (bytes)", GC.GetTotalMemory(forceFullCollection: false).ToHumanReadableBytes()),
                ("Total RAM usage (bytes)", currentProcess.WorkingSet64.ToHumanReadableBytes()),
                ("Virtual memory size (bytes)", currentProcess.VirtualMemorySize64.ToHumanReadableBytes()),
                ("User time", currentProcess.UserProcessorTime.ToString()),
                ("Total time", currentProcess.TotalProcessorTime.ToString())
            };
            channel.Display(
                new Table<(string, string)>
                {
                    Columns = new List<(string, Func<(string, string), string>)>
                    {
                        ("Metric", item => item.Item1),
                        ("Value", item => item.Item2)
                    },
                    Rows = performanceResult.ToList()
                }
            );
            return ExecuteStatus.Ok.ToExecutionResult();
        }
    }
}
