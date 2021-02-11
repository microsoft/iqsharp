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
        public SimulateNoiseMagic(ISymbolResolver resolver, IConfigurationSource configurationSource, INoiseModelSource noiseModelSource, ILogger<SimulateNoiseMagic> logger) : base(
            "simulate_noise",
            new Microsoft.Jupyter.Core.Documentation
            {
                Summary = "TODO",
                Description = "TODO",
                Examples = new string[]
                {
                }
            })
        {
            this.SymbolResolver = resolver;
            this.ConfigurationSource = configurationSource;
            this.Logger = logger;
            this.NoiseModelSource = noiseModelSource;
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

            var qsim = new OpenSystemsSimulator();
            if (NoiseModelSource.NoiseModel != null)
            {
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
