// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.Simulation.Simulators;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    public class EstimateMagic : AbstractMagic
    {
        public EstimateMagic(ISymbolResolver resolver) : base(
            "estimate",
            new Documentation {
                Summary = "Runs a given function or operation on the QuantumSimulator target machine"
            })
        {
            this.SymbolResolver = resolver;
        }

        public ISymbolResolver SymbolResolver { get; }

        public override ExecutionResult Run(string input, IChannel channel) =>
            RunAsync(input, channel).Result;


        public async Task<ExecutionResult> RunAsync(string input, IChannel channel)
        {
            var (name, args) = ParseInput(input);

            var symbol = SymbolResolver.Resolve(name) as IQSharpSymbol;
            if (symbol == null) throw new InvalidOperationException($"Invalid operation name: {name}");

            var qsim = new ResourcesEstimator();
            qsim.DisableLogToConsole();

            await symbol.Operation.RunAsync(qsim, args);

            return qsim.ToExecutionResult();
        }
    }
}
