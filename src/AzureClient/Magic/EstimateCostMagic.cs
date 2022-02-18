// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Quantum.Jobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.Simulation.Simulators;
using Microsoft.Quantum.Simulation.Simulators.QCTraceSimulators;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    // NB: Since classes in the Azure.Quantum.Jobs.Models namespace are
    //     autogenerated with read-only non-virtual properties, we cannot
    //     create an instance of either CostEstimate or UsageEvent with values
    //     populated by the trace simulator. Our approach instead is to make
    //     new classes with a similar API, allowing for easy porting of display
    //     and test code when cost estimation functionality is moved to the
    //     service.
    #if NET6_0_OR_GREATER
    internal record SimulatedCostEstimate
    #else
    internal class SimulatedCostEstimate
    #endif
    {
        public string CurrencyCode { get; internal set; }
        public IReadOnlyList<SimulatedUsageEvent> Events { get; internal set; }
        public float? EstimatedTotal { get; internal set; }
    }

    #if NET6_0_OR_GREATER
    internal record SimulatedUsageEvent
    #else
    internal class SimulatedUsageEvent
    #endif
    {
        public string DimensionId { get; internal set; }
        public string DimensionName { get; internal set; }
        public string MeasureUnit { get; internal set; }
        public float? AmountBilled { get; internal set; }
        public float? AmountConsumed { get; internal set; }
        public float? UnitPrice { get; internal set; }
    }

    // NB: We need to subclass QCTraceSimulator to be able to get specific
    //     metrics when the operation type is not available as a compile-time
    //     type parameter.
    internal class CostEstimator : QCTraceSimulator
    {
        internal CostEstimator() : base(new QCTraceSimulatorConfiguration
            {
                UsePrimitiveOperationsCounter = true,
                ThrowOnUnconstrainedMeasurement = false
            })
        {}
        internal int GetN1QGates(string operationName) => (int)(
            this.GetMetric(operationName, PrimitiveOperationsGroupsNames.QubitClifford) +
            this.GetMetric(operationName, PrimitiveOperationsGroupsNames.R)+
            this.GetMetric(operationName, PrimitiveOperationsGroupsNames.T));

        internal int GetN2QGates(string operationName) => (int)(
            this.GetMetric(operationName, PrimitiveOperationsGroupsNames.CNOT));

        internal int GetNMeasurements(string operationName) => (int)(
            this.GetMetric(operationName, PrimitiveOperationsGroupsNames.Measure));
    }

    /// <summary>
    ///     A magic command that performs resource estimation on functions and
    ///     operations.
    /// </summary>
    public class EstimateCostMagic : AbstractMagic
    {
        private const string ParameterNameOperationName = "__operationName__";
        private const string TargetParameterName = "--target";
        private const string NShotsParameterName = "--n-shots";

        /// <summary>
        ///     Given a symbol resolver that can be used to locate operations,
        ///     constructs a new magic command that performs resource estimation
        ///     on resolved operations.
        /// </summary>
        public EstimateCostMagic(ISymbolResolver resolver, IAzureClient client, ILogger<EstimateCostMagic> logger) : base(
            "azure.estimate_cost",
            new Microsoft.Jupyter.Core.Documentation
            {
                Summary = "TODO",
                Description = @"
                    TODO

                    #### Required parameters

                    - Q# operation or function name. This must be the first parameter, and must be a valid Q# operation
                    or function name that has been defined either in the notebook or in a Q# file in the same folder.
                    - Arguments for the Q# operation or function must also be specified as `key=value` pairs.
                ".Dedent(),
                Examples = new []
                {
                    @"
                        TODO
                    ".Dedent(),
                }
            }, logger)
        {
            this.SymbolResolver = resolver;
            this.Client = client;
        }

        /// <summary>
        ///     The symbol resolver that this magic command uses to locate
        ///     operations.
        /// </summary>
        public ISymbolResolver SymbolResolver { get; }

        private IAzureClient Client { get; }

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
            var allParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNameOperationName);
            var (inputParameters, specialParameters) = SplitDashedParameters(allParameters);

            var name = inputParameters.DecodeParameter<string>(ParameterNameOperationName);
            var symbol = SymbolResolver.Resolve(name) as IQSharpSymbol;
            if (symbol == null) throw new InvalidOperationException($"Invalid operation name: {name}");

            // NB: We explicitly disable all output here, since we only want
            //     the approximate cost out at the end.
            var qsim = new CostEstimator();
            qsim.DisableLogToConsole();

            await symbol.Operation.RunAsync(qsim, inputParameters);

            // TODO: Take --target and other args here.
            var target = specialParameters.DecodeParameter<string>(TargetParameterName, Client.ActiveTargetId);
            var nShots = specialParameters.DecodeParameter<int>(NShotsParameterName, 1_000);
            var ionQ1QPrice = 0.00003F;
            var ionQ2QPrice = 0.0003F;
            var ionQMinPrice = 1.0F;

            // Extract data from the trace simulator.
            var n1QGates = qsim.GetN1QGates(symbol.Operation.RoslynType.FullName);
            var n2QGates = qsim.GetN2QGates(symbol.Operation.RoslynType.FullName);
            var nMeasurements = qsim.GetNMeasurements(symbol.Operation.RoslynType.FullName);

            var targetProvider = AzureExecutionTarget.GetProvider(target);
            var events = new List<SimulatedUsageEvent>();
            var costEstimate = new SimulatedCostEstimate
            {
                CurrencyCode = "USD",
                Events = events
            };

            switch (targetProvider)
            {
                case AzureProvider.IonQ:
                    if (target == "ionq.simulator")
                    {
                        costEstimate.EstimatedTotal = 0;
                    }
                    else
                    {
                        costEstimate.EstimatedTotal = System.Math.Max(
                            ionQMinPrice,
                            (
                                ionQ1QPrice * n1QGates +
                                ionQ2QPrice * n2QGates
                            ) * nShots
                        );
                    }
                    events.Add(new SimulatedUsageEvent
                    {
                        DimensionId = "gs1q",
                        DimensionName = "1Q Gate Shot",
                        MeasureUnit = "1q gate shot",
                        AmountBilled = 0,
                        AmountConsumed = (float)(n1QGates * nShots),
                        UnitPrice = 0
                    });
                    events.Add(new SimulatedUsageEvent
                    {
                        DimensionId = "gs2q",
                        DimensionName = "2Q Gate Shot",
                        MeasureUnit = "2q gate shot",
                        AmountBilled = 0,
                        AmountConsumed = (float)(n2QGates * nShots),
                        UnitPrice = 0
                    });
                    break;

                case AzureProvider.Honeywell:
                    costEstimate.CurrencyCode = "HQC";
                    if (target.Contains("apival"))
                    {
                        costEstimate.EstimatedTotal = 0;
                    }
                    else
                    {
                        costEstimate.EstimatedTotal = 5 + nShots * (
                            n1QGates + 10 * n2QGates + 5 * nMeasurements
                        ) / 5000;
                    }
                    events.Add(new SimulatedUsageEvent
                    {
                        DimensionId = "gates1q",
                        DimensionName = "1Q Gates",
                        MeasureUnit = "1q gates",
                        AmountBilled = 0,
                        AmountConsumed = (float)(n1QGates * nShots),
                        UnitPrice = 0
                    });
                    events.Add(new SimulatedUsageEvent
                    {
                        DimensionId = "gates2q",
                        DimensionName = "2Q Gates",
                        MeasureUnit = "2q gates",
                        AmountBilled = 0,
                        AmountConsumed = (float)(n2QGates * nShots),
                        UnitPrice = 0
                    });
                    events.Add(new SimulatedUsageEvent
                    {
                        DimensionId = "measops",
                        DimensionName = "Measurement operations",
                        MeasureUnit = "measurement operations",
                        AmountBilled = 0,
                        AmountConsumed = (float)(nMeasurements * nShots),
                        UnitPrice = 0
                    });
                    break;

                default:
                    channel.Stderr($"Cost estimation is not supported for target \"{target}\".");
                    return ExecuteStatus.Error.ToExecutionResult();
            }

            return costEstimate.ToExecutionResult();
        }
    }
}
