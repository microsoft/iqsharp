// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Microsoft.Quantum.IQSharp.Core.ExecutionPathTracer;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.Simulation.Simulators;
using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    ///      Contains the JSON representation of the <see cref="ExecutionPath"/>
    ///      and the ID of the HTML div that will contain the visualization.
    /// </summary>
    public class ExecutionPathVisualizerContent : MessageContent
    {
        /// <summary>
        ///     Initializes <see cref="ExecutionPathVisualizerContent"/> with the
        ///     given JSON string and HTML div ID.
        /// </summary>
        public ExecutionPathVisualizerContent(string json, string id)
        {
            this.Json = json;
            this.Id = id;
        }

        /// <summary>
        ///     JSON representation of the <see cref="ExecutionPath"/>.
        /// </summary>
        [JsonProperty("json")]
        public string Json { get; }

        /// <summary>
        ///     ID of the HTML div that will contain the visualization.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; }
    }

    /// <summary>
    ///     A magic command that can be used to visualize the execution
    ///     paths of operations and functions.
    /// </summary>
    public class ViewMagic : AbstractMagic
    {
        private const string ParameterNameOperationName = "__operationName__";

        /// <summary>
        ///     Constructs a new magic command given a resolver used to find
        ///     operations and functions, and a configuration source used to set
        ///     configuration options.
        /// </summary>
        public ViewMagic(ISymbolResolver resolver, IConfigurationSource configurationSource) : base(
            "view",
            new Documentation
            {
                Summary = "Outputs an HTML-based visualization of the execution path of a given operation."
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
        ///     Outputs a visualization of an operation given a string with its name and a JSON
        ///     encoding of its arguments.
        /// </summary>
        public async Task<ExecutionResult> RunAsync(string input, IChannel channel)
        {
            // Parse input parameters
            var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNameOperationName);

            var name = inputParameters.DecodeParameter<string>(ParameterNameOperationName);
            var symbol = SymbolResolver.Resolve(name) as IQSharpSymbol;
            if (symbol == null) throw new InvalidOperationException($"Invalid operation name: {name}");

            var tracer = new ExecutionPathTracer();

            // Simulate operation and attach `ExecutionPathTracer` to keep track of operations performed
            // in its execution path
            using var qsim = new QuantumSimulator()
                .WithJupyterDisplay(channel, ConfigurationSource)
                .WithStackTraceDisplay(channel)
                .WithExecutionPathTracer(tracer);
            var value = await symbol.Operation.RunAsync(qsim, inputParameters);

            // Retrieve the `ExecutionPath` traced out by the `ExecutionPathTracer`
            var executionPath = tracer.GetExecutionPath();

            // Render empty div with unique ID as cell output
            var divId = $"execution-path-container-{Guid.NewGuid().ToString()}";
            var content = new ExecutionPathVisualizerContent(executionPath.ToJson(), divId);
            channel.DisplayUpdatable(new ExecutionPathDisplayable(content.Id));

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
