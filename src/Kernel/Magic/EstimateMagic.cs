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
        /// <summary>
        ///     Given a symbol resolver that can be used to locate operations,
        ///     constructs a new magic command that performs resource estimation
        ///     on resolved operations.
        /// </summary>
        public EstimateMagic(ISymbolResolver resolver) : base(
            "estimate",
            new Documentation
            {
                Summary = "Runs a given function or operation on the ResourcesEstimator target machine."
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
            var (name, args) = ParseInput(input);

            var symbol = SymbolResolver.Resolve(name) as IQSharpSymbol;
            if (symbol == null) throw new InvalidOperationException($"Invalid operation name: {name}");

            var qsim = new ResourcesEstimator().WithStackTraceDisplay(channel);
            qsim.DisableLogToConsole();

            await symbol.Operation.RunAsync(qsim, args);

            return qsim.Data.ToExecutionResult();
        }
    }
}
