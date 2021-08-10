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
using Microsoft.Quantum.IQSharp.Kernel;

namespace Microsoft.Quantum.Experimental
{
    public class NoiseModelMagic : AbstractMagic
    {
        private ILogger<NoiseModelMagic>? logger = null;
        private INoiseModelSource NoiseModelSource;

        /// <summary>
        ///     Allows for querying noise models and for loading new noise models.
        /// </summary>
        public NoiseModelMagic(IExecutionEngine engine, INoiseModelSource noiseModelSource, ILogger<NoiseModelMagic> logger) : base(
            "experimental.noise_model",
            new Microsoft.Jupyter.Core.Documentation
            {
                Summary = "Gets, sets, saves, or loads a noise model used in simulating quantum operations.",
                Description = @"
                    > **⚠ WARNING:** This magic command is **experimental**,
                    > is not supported, and may be removed from future versions without notice.

                    This magic command allows accessing or modifying the noise model used by
                    the `%experimental.simulate_noise` magic command.
                ".Dedent(),
                Examples = new string[]
                {
                    @"
                        Return the currently set noise model:
                        ```
                        In []: %experimental.noise_model
                        ```
                    ".Dedent(),
                    @"
                        Return the built-in noise model with a given name:
                        ```
                        In []: %experimental.noise_model --get-by-name ideal
                        ```
                    ",
                    @"
                        Sets the noise model to a built-in named noise model:
                        ```
                        In []: %experimental.noise_model --load-by-name ideal_stabilizer
                        ```
                    ".Dedent(),
                    @"
                        Set the noise model to a noise model given as JSON:
                        ```
                        In []: %experimental.noise_model { ... }
                        ```
                    ".Dedent(),
                    @"
                        Save the current noise model to a JSON file named
                        `noise-model.json`:
                        ```
                        In []: %experimental.noise_model --save noise-model.json
                        ```
                    ".Dedent(),
                    @"
                        Load the noise model stored in `noise-model.json`,
                        making it the active noise model:
                        ```
                        In []: %experimental.noise_model --load noise-model.json
                        ```
                    ".Dedent()
                }
            })
        {
            this.NoiseModelSource = noiseModelSource;
            if (engine is IQSharpEngine iQSharpEngine)
            {
                iQSharpEngine.RegisterDisplayEncoder(new NoiseModelToHtmlDisplayEncoder());
            }
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
            else if (command.Trim() == "--get-by-name")
            {
                var name = parts[1];
                if (NoiseModel.TryGetByName(name, out var noiseModel))
                {
                    return noiseModel.ToExecutionResult();
                }
                else
                {
                    return $"No built-in noise model with name {name}.".ToExecutionResult(ExecuteStatus.Error);
                }
            }
            else if (command.Trim() == "--load-by-name")
            {
                var name = parts[1];
                if (NoiseModel.TryGetByName(name, out var noiseModel))
                {
                    NoiseModelSource.NoiseModel = noiseModel;
                }
                else
                {
                    return $"No built-in noise model with name {name}.".ToExecutionResult(ExecuteStatus.Error);
                }
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
