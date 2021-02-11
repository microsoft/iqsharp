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

namespace Microsoft.Quantum.Experimental
{
    public class NoiseModelMagic : AbstractMagic
    {
        private ILogger<NoiseModelMagic>? logger = null;
        private INoiseModelSource NoiseModelSource;

        /// <summary>
        ///     Allows for querying noise models and for loading new noise models.
        /// </summary>
        public NoiseModelMagic(INoiseModelSource noiseModelSource, ILogger<NoiseModelMagic> logger) : base(
            "experimental.noise_model",
            new Microsoft.Jupyter.Core.Documentation
            {
                Summary = "TODO",
                Description = "TODO",
                Examples = new string[]
                {
                }
            })
        {
            this.NoiseModelSource = noiseModelSource;
        }

        /// <inheritdoc />
        public override ExecutionResult Run(string input, IChannel channel)
        {
            var parts = input.Trim().Split(" ", 2);
            var command = parts[0];
            if (command.Trim() == "--save")
            {
                if (NoiseModelSource.NoiseModel == null)
                {
                    channel.Stderr("No noise model set; nothing to save.");
                }
                else
                {
                    var filename = parts[1];
                    File.WriteAllText(filename, JsonSerializer.Serialize(NoiseModelSource.NoiseModel));
                }
            }
            else if (command.Trim() == "--load")
            {
                var filename = parts[1];
                NoiseModelSource.NoiseModel = JsonSerializer.Deserialize<NoiseModel>(File.ReadAllText(filename));
            }
            else if (input.Trim().StartsWith("{"))
            {
                // Parse the input as JSON.
                NoiseModelSource.NoiseModel = JsonSerializer.Deserialize<NoiseModel>(input.Trim());
            }
            else
            {
                // Just return the existing noise model.
                return NoiseModelSource.NoiseModel.ToExecutionResult();
            }

            return ExecuteStatus.Ok.ToExecutionResult();
        }
    }
}
