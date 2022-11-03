// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    ///     A magic command that can be used to simulate operations and functions
    ///     on a full-state quantum simulator.
    /// </summary>
    public class SimulateMagic : AbstractNativeSimulateMagic
    {
        /// <summary>
        ///     Constructs a new magic command given a resolver used to find
        ///     operations and functions, and a configuration source used to set
        ///     configuration options.
        /// </summary>
        public SimulateMagic(ISymbolResolver resolver, IConfigurationSource configurationSource, IPerformanceMonitor monitor, ILogger<SimulateMagic> logger) : base(
            "simulate",
            new Microsoft.Jupyter.Core.Documentation
            {
                Summary = "Runs a given function or operation on the QuantumSimulator target machine.",
                Description = @"
                    This magic command allows executing a given function or operation on the QuantumSimulator, 
                    which performs a full-state simulation of the given function or operation
                    and prints the resulting return value.

                    See the [QuantumSimulator user guide](https://docs.microsoft.com/azure/quantum/user-guide/machines/full-state-simulator) to learn more.

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
            }, resolver, configurationSource, monitor, logger)
        {
        }

        internal override CommonNativeSimulator CreateNativeSimulator() => new QuantumSimulator();
    }
}
