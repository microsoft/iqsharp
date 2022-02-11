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
    ///     Abstract class for magic commands that can be used to simulate
    ///     operations and functions on a full-state quantum simulator, using
    ///     a common C API.
    /// </summary>
    public abstract class AbstractNativeSimulateMagic : AbstractMagic
    {
        private const string ParameterNameOperationName = "__operationName__";
        private readonly IPerformanceMonitor Monitor;

        /// <summary>
        ///     Constructs a new magic command given a resolver used to find
        ///     operations and functions, and a configuration source used to set
        ///     configuration options.
        /// </summary>
        public AbstractNativeSimulateMagic(string keyword, Documentation docs, ISymbolResolver resolver, IConfigurationSource configurationSource, IPerformanceMonitor monitor, ILogger<AbstractNativeSimulateMagic> logger)
        : base(keyword, docs, logger)
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

        internal abstract CommonNativeSimulator CreateNativeSimulator();

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

            using var qsim = CreateNativeSimulator()
                .WithStackTraceDisplay(channel);

            qsim.DisableLogToConsole();
            qsim.OnLog += channel.Stdout;

            qsim.OnDisplayableDiagnostic += (displayable) =>
            {
                if (displayable is CommonNativeSimulator.DisplayableState state && ConfigurationSource.MeasurementDisplayHistogram)
                {
                    // Make sure to display the state first so that it's there for the client-side
                    // JavaScript to pick up.
                    var id = $"{System.Guid.NewGuid()}";
                    channel.Display(new DisplayableStateWithId
                    {
                        Amplitudes = state.Amplitudes,
                        NQubits = state.NQubits,
                        QubitIds = state.QubitIds,
                        Id = id
                    });

                    // Tell the client to add a histogram using chart.js.
                    var commsRouter = channel.GetCommsRouter();
                    Debug.Assert(commsRouter != null, "Histogram display requires comms router.");
                    commsRouter.OpenSession(
                        "iqsharp_state_dump",
                        new MeasurementHistogramContent()
                        {
                            State = state,
                            Id = id
                        }
                    ).Wait();
                }
                else
                {
                    channel.Display(displayable);
                }
            };

            qsim.AfterAllocateQubits += (args) =>
            {
                maxNQubits = System.Math.Max(qsim.QubitManager?.AllocatedQubitsCount ?? 0, maxNQubits);
            };
            var stopwatch = Stopwatch.StartNew();
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
