// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.IQSharp.Kernel;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;

namespace Microsoft.Quantum.Experimental
{
    public class SimulateNoiseMagic : AbstractMagic
    {
        private const string ParameterNameOperationName = "__operationName__";
        private ILogger<SimulateNoiseMagic>? Logger = null;
        private readonly INoiseModelSource NoiseModelSource;

        /// <summary>
        ///     Constructs a new magic command given a resolver used to find
        ///     operations and functions, and a configuration source used to set
        ///     configuration options.
        /// </summary>
        public SimulateNoiseMagic(IExecutionEngine engine, ISymbolResolver resolver, IConfigurationSource configurationSource, INoiseModelSource noiseModelSource, ILogger<SimulateNoiseMagic> logger) : base(
            "experimental.simulate_noise",
            new Microsoft.Jupyter.Core.Documentation
            {
                Summary = "Runs a given function or operation on the OpenSystemsSimulator target machine.",
                Description = $@"
                    > **⚠ WARNING:** This magic command is **experimental**,
                    > is not supported, and may be removed from future versions without notice.

                    This magic command allows executing a given function or operation
                    on the OpenSystemsSimulator target, simulating how that function or operation
                    will perform when run on noisy quantum hardware.

                    #### See also

                    - [`%config`]({KnownUris.ReferenceForMagicCommand("config")})
                    - [`%experimental.noise_model`]({KnownUris.ReferenceForMagicCommand("experimental.noise_model")})

                    #### Required parameters

                    - Q# operation or function name. This must be the first parameter, and must be a valid Q# operation
                    or function name that has been defined either in the notebook or in a Q# file in the same folder.
                    - Arguments for the Q# operation or function must also be specified as `key=value` pairs.

                    #### Remarks

                    The behavior of this magic command can be controlled through the `%experimental.noise_model` magic command,
                    and the `opensim.nQubits` and `opensim.representation` configuration settings.
                ".Dedent(),
                Examples = new string[]
                {
                    @"
                        Simulate a Q# operation defined as `operation MyOperation() : Result`:
                        ```
                        In []: %simulate MyOperation
                        Out[]: <return value of the operation>
                        ```
                    ".Dedent(),
                    @"
                        Simulate a Q# operation defined as `operation MyOperation(a : Int, b : Int) : Result`:
                        ```
                        In []: %simulate MyOperation a=5 b=10
                        Out[]: <return value of the operation>
                        ```
                    ".Dedent(),
                }
            })
        {
            this.SymbolResolver = resolver;
            this.ConfigurationSource = configurationSource;
            this.Logger = logger;
            this.NoiseModelSource = noiseModelSource;

            if (engine is IQSharpEngine iQSharpEngine)
            {
                iQSharpEngine.RegisterDisplayEncoder(new MixedStateToHtmlDisplayEncoder());
                iQSharpEngine.RegisterDisplayEncoder(new StabilizerStateToHtmlDisplayEncoder(configurationSource));
            }
        }

        /// <summary>
        ///      The symbol resolver used by this magic command to find
        ///      operations or functions to be simulated.
        /// </summary>
        public ISymbolResolver SymbolResolver { get; }

        /// <summary>
        ///     The configuration source used by this magic command to control
        ///     simulation options (e.g.: dump formatting options).
        /// </summary>
        public IConfigurationSource ConfigurationSource { get; }

        /// <inheritdoc />
        public override ExecutionResult Run(string input, IChannel channel) =>
            RunAsync(input, channel).Result;

        /// <summary>
        ///     Simulates an operation given a string with its name and a JSON
        ///     encoding of its arguments.
        /// </summary>
        public async Task<ExecutionResult> RunAsync(string input, IChannel channel)
        {
            var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNameOperationName);

            var name = inputParameters.DecodeParameter<string>(ParameterNameOperationName);
            var symbol = SymbolResolver.Resolve(name) as dynamic; // FIXME: Should be IQSharpSymbol.
            if (symbol == null) throw new InvalidOperationException($"Invalid operation name: {name}");

            var qsim = new OpenSystemsSimulator(
                ConfigurationSource.ExperimentalSimulatorCapacity,
                ConfigurationSource.ExperimentalSimulatorRepresentation
            );
            if (NoiseModelSource.NoiseModel != null)
            {
                var json = JsonSerializer.Serialize(NoiseModelSource.NoiseModel);
                Console.WriteLine(json);
                qsim.NoiseModel = NoiseModelSource.NoiseModel;
            }
            Logger?.LogDebug("Simulating with noise model: {NoiseModel}", JsonSerializer.Serialize(NoiseModelSource.NoiseModel));
            qsim.DisableLogToConsole();
            qsim.OnLog += channel.Stdout;
            qsim.OnDisplayableDiagnostic += channel.Display;
            var operation = symbol.Operation as OperationInfo;
            var value = await operation.RunAsync(qsim, inputParameters);
            return value.ToExecutionResult();
        }
    }
}
