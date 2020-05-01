// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Kernel;
using Microsoft.Quantum.Simulation.Simulators;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    /// Runs a given function or operation on the ToffoliSimulator target machine.
    /// </summary>
    public class ToffoliMagic : AbstractMagic
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public ToffoliMagic(ISymbolResolver resolver) : base(
            "toffoli",
            new Documentation {
                Summary = "Runs a given function or operation on the ToffoliSimulator simulator target machine"
            })
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
            var (name, args) = ParseInput(input);

            var symbol = SymbolResolver.Resolve(name) as IQSharpSymbol;
            if (symbol == null) throw new InvalidOperationException($"Invalid operation name: {name}");

            var qsim = new ToffoliSimulator().WithStackTraceDisplay(channel);
            qsim.DisableLogToConsole();
            qsim.OnLog += channel.Stdout;

            var value = await symbol.Operation.RunAsync(qsim, args);

            return value.ToExecutionResult();
        }
    }
}
