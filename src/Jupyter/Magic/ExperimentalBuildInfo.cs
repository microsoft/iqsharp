// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Microsoft.Quantum.Experimental
{
    public class ExperimentalBuildInfoMagic : AbstractMagic
    {

        /// <summary>
        ///     Allows for querying noise models and for loading new noise models.
        /// </summary>
        public ExperimentalBuildInfoMagic() : base(
            "experimental.build_info",
            new Microsoft.Jupyter.Core.Documentation
            {
                Summary = "Reports build info for the experimental simulators.",
                Description = @"
                    > **⚠ WARNING:** This magic command is **experimental**,
                    > is not supported, and may be removed from future versions without notice.
                ".Dedent(),
                Examples = new string[]
                {
                    @"
                        Return the build info for experimental simulators:
                        ```
                        In []: %experimental.build_info
                        ```
                    ".Dedent(),
                }
            })
        { }

        /// <inheritdoc />
        public override ExecutionResult Run(string input, IChannel channel)
        {
            return OpenSystemsSimulator.BuildInfo.ToExecutionResult();
        }
    }
}
