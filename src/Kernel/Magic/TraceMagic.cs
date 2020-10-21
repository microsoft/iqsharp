// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Microsoft.Quantum.IQSharp.ExecutionPathTracer;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.Simulation.Simulators;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    ///      Contains the JSON representation of the <see cref="ExecutionPath"/>
    ///      and metadata used in the visualization of the execution path.
    /// </summary>
    public class ExecutionPathVisualizerContent : MessageContent
    {
        /// <summary>
        ///     Initializes <see cref="ExecutionPathVisualizerContent"/> with the
        ///     given <see cref="ExecutionPath"/>.
        /// </summary>
        /// <param name="executionPath">
        ///     <see cref="ExecutionPath"/> (as a <see cref="JToken"/>) to be visualized.
        /// </param>
        /// <param name="id">
        ///     HTML div ID to inject visualization into.
        /// </param>
        /// <param name="renderDepth">
        ///     The initial renderDepth at which to visualize the execution path.
        /// </param>
        /// <param name="style">
        ///     The  <see cref="TraceVisualizationStyle"/> for visualizing the execution path.
        /// </param>
        public ExecutionPathVisualizerContent(JToken executionPath, string id, int renderDepth, TraceVisualizationStyle style)
        {
            this.ExecutionPath = executionPath;
            this.Id = id;
            this.Style = style.ToString();
            this.RenderDepth = renderDepth;
        }

        /// <summary>
        ///     The <see cref="ExecutionPath"/> (as a <see cref="JToken"/>) to be rendered.
        /// </summary>
        [JsonProperty("executionPath")]
        public JToken ExecutionPath { get; }

        /// <summary>
        ///     ID of the HTML div that will contain the visualization.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; }

        /// <summary>
        ///     Style for visualization.
        /// </summary>
        [JsonProperty("style")]
        public string Style { get; }

        /// <summary>
        ///     Initial depth at which to render operations used in the execution path.
        /// </summary>
        [JsonProperty("renderDepth")]
        public int RenderDepth { get; }
    }

    /// <summary>
    ///     A magic command that can be used to visualize the execution
    ///     path of operations and functions traced out by the simulator.
    /// </summary>
    public class TraceMagic : AbstractMagic
    {
        private const string ParameterNameOperationName = "__operationName__";
        private const string ParameterNameDepth = "--depth";

        /// <summary>
        ///     Constructs a new magic command given a resolver used to find
        ///     operations and functions, and a configuration source used to set
        ///     configuration options.
        /// </summary>
        public TraceMagic(ISymbolResolver resolver, IConfigurationSource configurationSource) : base(
            "trace",
            new Microsoft.Jupyter.Core.Documentation
            {
                Summary = "Visualizes the execution path of the given operation.",
                Description = $@"
                    This magic command renders an HTML-based visualization of a runtime execution path of the
                    given operation using the QuantumSimulator.

                    #### Required parameters

                    - Q# operation or function name. This must be the first parameter, and must be a valid Q# operation
                    or function name that has been defined either in the notebook or in a Q# file in the same folder.
                    - Arguments for the Q# operation or function must also be specified as `key=value` pairs.

                    #### Optional parameters

                    - `{ParameterNameDepth}=<integer>` (default=1): The depth at which to render operations along
                    the execution path.
                ".Dedent(),
                Examples = new[]
                {
                    @"
                        Visualize the execution path of a Q# operation defined as `operation MyOperation() : Result`:
                        ```
                        In []: %trace MyOperation
                        Out[]: <HTML visualization of the operation>
                        ```
                    ".Dedent(),
                    @"
                        Visualize the execution path of a Q# operation defined as `operation MyOperation(a : Int, b : Int) : Result`:
                        ```
                        In []: %trace MyOperation a=5 b=10
                        Out[]: <HTML visualization of the operation>
                        ```
                    ".Dedent(),
                    $@"
                        Visualize operations at depth 2 on the execution path of a Q# operation defined
                        as `operation MyOperation() : Result`:
                        ```
                        In []: %trace MyOperation {ParameterNameDepth}=2
                        Out[]: <HTML visualization of the operation>
                        ```
                    ".Dedent(),
                }
            })
        {
            this.SymbolResolver = resolver;
            this.ConfigurationSource = configurationSource;
        }

        /// <summary>
        ///      The symbol resolver used by this magic command to find the operation
        ///      to be visualized.
        /// </summary>
        public ISymbolResolver SymbolResolver { get; }

        /// <summary>
        ///     The configuration source used by this magic command to control
        ///     visualization options.
        /// </summary>
        public IConfigurationSource ConfigurationSource { get; }

        /// <inheritdoc />
        public override ExecutionResult Run(string input, IChannel channel) =>
            RunAsync(input, channel).Result;

        /// <summary>
        ///     Outputs a visualization of a runtime execution path of an operation given
        ///     a string with its name and a JSON encoding of its arguments.
        /// </summary>
        public async Task<ExecutionResult> RunAsync(string input, IChannel channel)
        {
            // Parse input parameters
            var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNameOperationName);

            var name = inputParameters.DecodeParameter<string>(ParameterNameOperationName);
            var symbol = SymbolResolver.Resolve(name) as IQSharpSymbol;
            if (symbol == null) throw new InvalidOperationException($"Invalid operation name: {name}");

            var depth = inputParameters.DecodeParameter<int>(
                ParameterNameDepth,
                defaultValue: this.ConfigurationSource.TraceVisualizationDefaultDepth
            );
            if (depth <= 0) throw new ArgumentOutOfRangeException($"Invalid depth: {depth}. Must be >= 1.");

            var tracer = new ExecutionPathTracer.ExecutionPathTracer();

            // Simulate operation and attach `ExecutionPathTracer` to trace out operations performed
            // in its execution path
            using var qsim = new QuantumSimulator()
                .WithJupyterDisplay(channel, ConfigurationSource)
                .WithStackTraceDisplay(channel)
                .WithExecutionPathTracer(tracer);
            var value = await symbol.Operation.RunAsync(qsim, inputParameters);

            // Retrieve the `ExecutionPath` traced out by the `ExecutionPathTracer`
            var executionPath = tracer.GetExecutionPath();

            // Convert executionPath to JToken for serialization
            var executionPathJToken = JToken.FromObject(executionPath,
                new JsonSerializer() { NullValueHandling = NullValueHandling.Ignore });

            // Render empty div with unique ID as cell output
            var divId = $"execution-path-container-{Guid.NewGuid().ToString()}";
            var content = new ExecutionPathVisualizerContent(
                executionPathJToken,
                divId,
                depth,
                this.ConfigurationSource.TraceVisualizationStyle
            );
            channel.DisplayUpdatable(new DisplayableHtmlElement($"<div id='{divId}' />"));

            // Send execution path to JavaScript via iopub for rendering
            channel.SendIoPubMessage(
                new Message
                {
                    Header = new MessageHeader
                    {
                        MessageType = "render_execution_path"
                    },
                    Content = content,
                }
            );

            return ExecuteStatus.Ok.ToExecutionResult();
        }
    }
}
