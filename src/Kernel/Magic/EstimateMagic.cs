// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.IQSharp.Kernel;
using Microsoft.Quantum.Simulation.Simulators;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    ///     A magic command that performs resource estimation on functions and
    ///     operations.
    /// </summary>
    public class EstimateMagic : AbstractMagic
    {
        private const string ParameterNameOperationName = "__operationName__";

        /// <summary>
        ///     Given a symbol resolver that can be used to locate operations,
        ///     constructs a new magic command that performs resource estimation
        ///     on resolved operations.
        /// </summary>
        public EstimateMagic(ISymbolResolver resolver) : base(
            "estimate",
            new Documentation {
                Summary = "Runs a given function or operation on the ResourcesEstimator target machine.",
                Description = @"
                    The ResourcesEstimator estimates statistics about how many resources the given
                    operation needs for execution. The resources it calculates include:
                    - Counts for each primitive operation
                    - Depth (lower bound for the T-gate depth of the quantum circuit)
                    - Width (lower bound for the maximum number of qubits used for the computation)

                    #### Required parameters

                    - Q# operation or function name. This must be the first parameter, and must be a valid Q# operation
                    or function name that has been defined either in the notebook or in a Q# file in the same folder.
                    - Arguments for the Q# operation or function must also be specified as `key=value` pairs.
                ".Dedent(),
                Examples = new []
                {
                    @"
                        Estimate resources for a Q# operation defined as `operation MyOperation(a : Int, b : Int) : Result`:
                        ```
                        In []: %estimate MyOperation a=5 b=10
                        Out[]: Metric           Sum     
                               ---------------- ----
                               CNOT             0
                               QubitClifford    4
                               R                0
                               Measure          8
                               T                0
                               Depth            0
                               Width            4
                               BorrowedWidth    0
                        ```
                    ".Dedent(),
                }
            })
        {
            this.SymbolResolver = resolver;
        }

        /// <summary>
        ///     The symbol resolver that this magic command uses to locate
        ///     operations.
        /// </summary>
        public ISymbolResolver SymbolResolver { get; }

        /// <inheritdoc />
        public override ExecutionResult Run(string input, IChannel channel) =>
            RunAsync(input, channel).Result;


        /// <summary>
        ///     Given an input representing the name of an operation and a JSON
        ///     serialization of its inputs, returns a task that can be awaited
        ///     on for resource estimates from running that operation.
        /// </summary>
        public async Task<ExecutionResult> RunAsync(string input, IChannel channel)
        {
            var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNameOperationName);

            var name = inputParameters.DecodeParameter<string>(ParameterNameOperationName);
            var symbol = SymbolResolver.Resolve(name) as IQSharpSymbol;
            if (symbol == null) throw new InvalidOperationException($"Invalid operation name: {name}");

            var qsim = new ResourcesEstimator().WithStackTraceDisplay(channel);
            qsim.DisableLogToConsole();

            await symbol.Operation.RunAsync(qsim, inputParameters);

            return qsim.Data.ToExecutionResult();
        }
    }
}
