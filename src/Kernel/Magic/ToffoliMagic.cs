// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.IQSharp.Kernel;
using Microsoft.Quantum.Simulation.Simulators;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    /// Runs a given function or operation on the ToffoliSimulator target machine.
    /// </summary>
    public class ToffoliMagic : AbstractMagic
    {
        private const string ParameterNameOperationName = "__operationName__";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ToffoliMagic(ISymbolResolver resolver, ILogger<ToffoliMagic> logger) : base(
            "toffoli",
            new Microsoft.Jupyter.Core.Documentation
            {
                Summary = "Runs a given function or operation on the ToffoliSimulator target machine.",
                Description = @"
                    This magic command allows executing a given function or operation on the ToffoliSimulator, 
                    which performs a simulation of the given function or operation in which the state is always
                    a simple product state in the computational basis, and prints the resulting return value.

                    See the [ToffoliSimulator user guide](https://docs.microsoft.com/azure/quantum/user-guide/machines/toffoli-simulator) to learn more.

                    #### Required parameters

                    - Q# operation or function name. This must be the first parameter, and must be a valid Q# operation
                    or function name that has been defined either in the notebook or in a Q# file in the same folder.
                    - Arguments for the Q# operation or function must also be specified as `key=value` pairs.
                ".Dedent(),
                Examples = new []
                {
                    @"
                        Use the ToffoliSimulator to simulate a Q# operation
                        defined as `operation MyOperation() : Result`:
                        ```
                        In []: %toffoli MyOperation
                        Out[]: <return value of the operation>
                        ```
                    ".Dedent(),
                    @"
                        Use the ToffoliSimulator to simulate a Q# operation
                        defined as `operation MyOperation(a : Int, b : Int) : Result`:
                        ```
                        In []: %toffoli MyOperation a=5 b=10
                        Out[]: <return value of the operation>
                        ```
                    ".Dedent(),
                }
            }, logger)
        {
            this.SymbolResolver = resolver;
        }

        /// <summary>
        /// ISumbolResolver used to find the function/operation to simulate.
        /// </summary>
        public ISymbolResolver SymbolResolver { get; }

        /// <inheritdoc />
        public override ExecutionResult Run(string input, IChannel channel) =>
            RunAsync(input, channel).Result;

        /// <summary>
        /// Simulates a function/operation using the ToffoliSimulator as target machine.
        /// It expects a single input: the name of the function/operation to simulate.
        /// </summary>
        public async Task<ExecutionResult> RunAsync(string input, IChannel channel)
        {
            var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNameOperationName);

            var name = inputParameters.DecodeParameter<string>(ParameterNameOperationName);
            var symbol = SymbolResolver.Resolve(name) as IQSharpSymbol;
            if (symbol == null) throw new InvalidOperationException($"Invalid operation name: {name}");

            var qsim = new ToffoliSimulator().WithStackTraceDisplay(channel);
            qsim.OnDisplayableDiagnostic += channel.Display;
            qsim.DisableLogToConsole();
            qsim.OnLog += channel.Stdout;

            var value = await symbol.Operation.RunAsync(qsim, inputParameters);

            return value.ToExecutionResult();
        }
    }
}
