// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    public class SimulateMagic : AbstractMagic
    {
        public SimulateMagic(ISymbolResolver resolver, IConfigurationSource configurationSource) : base(
            "simulate",
            new Documentation {
                Summary = "Runs a given function or operation on the QuantumSimulator target machine"
            })
        {
            this.SymbolResolver = resolver;
            this.ConfigurationSource = configurationSource;
        }

        public ISymbolResolver SymbolResolver { get; }
        public IConfigurationSource ConfigurationSource { get; }

        public override ExecutionResult Run(string input, IChannel channel) =>
            RunAsync(input, channel).Result;

        public async Task<ExecutionResult> RunAsync(string input, IChannel channel)
        {
            var (name, args) = ParseInput(input);

            var symbol = SymbolResolver.Resolve(name) as IQSharpSymbol;
            if (symbol == null) throw new InvalidOperationException($"Invalid operation name: {name}");

            using var qsim = new QuantumSimulator()
                .WithJupyterDisplay(channel, ConfigurationSource)
                .WithStackTraceDisplay(channel);
            var value = await symbol.Operation.RunAsync(qsim, args);
            return value.ToExecutionResult();
        }
    }
}
