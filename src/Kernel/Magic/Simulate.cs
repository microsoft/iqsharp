// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.IQSharp.Kernel;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    ///     A magic command that can be used to simulate operations and functions
    ///     on a full-state quantum simulator.
    /// </summary>
    public class SimulateMagic : AbstractMagic
    {
        private const string ParameterNameOperationName = "__operationName__";
        private readonly IPerformanceMonitor Monitor;

        /// <summary>
        ///     Constructs a new magic command given a resolver used to find
        ///     operations and functions, and a configuration source used to set
        ///     configuration options.
        /// </summary>
        public SimulateMagic(ISymbolResolver resolver, IConfigurationSource configurationSource, IPerformanceMonitor monitor) : base(
            "simulate",
            new Microsoft.Jupyter.Core.Documentation
            {
                Summary = "Runs a given function or operation on the QuantumSimulator target machine.",
                Description = @"
                    This magic command allows executing a given function or operation on the QuantumSimulator, 
                    which performs a full-state simulation of the given function or operation
                    and prints the resulting return value.

                    See the [QuantumSimulator user guide](https://docs.microsoft.com/quantum/user-guide/machines/full-state-simulator) to learn more.

                    #### Required parameters

                    - Q# operation or function name. This must be the first parameter, and must be a valid Q# operation
                    or function name that has been defined either in the notebook or in a Q# file in the same folder.
                    - Arguments for the Q# operation or function must also be specified as `key=value` pairs.
                ".Dedent(),
                Examples = new []
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
            this.Monitor = monitor;
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
            var symbol = SymbolResolver.Resolve(name) as IQSharpSymbol;
            if (symbol == null) throw new InvalidOperationException($"Invalid operation name: {name}");

            var maxNQubits = 0L;

            using var qsim = new QuantumSimulator()
                .WithJupyterDisplay(channel, ConfigurationSource)
                .WithStackTraceDisplay(channel);
            qsim.OnDisplayableDiagnostic += channel.Display;
            qsim.OnAllocateQubits += nQubits =>
            {
                maxNQubits = System.Math.Max(qsim.QubitManager.GetAllocatedQubitsCount(), maxNQubits);
            };
            var stopwatch = new Stopwatch();
            var value = await symbol.Operation.RunAsync(qsim, inputParameters);
            stopwatch.Stop();
            var result = value.ToExecutionResult();
            (Monitor as PerformanceMonitor)?.ReportSimulatorPerformance(new SimulatorPerformanceArgs(
                simulatorName: qsim.GetType().FullName,
                nQubits: (int)maxNQubits,
                duration: stopwatch.Elapsed
            ));
            return result;
        }
    }
}
