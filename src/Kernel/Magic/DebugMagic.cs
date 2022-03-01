// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Microsoft.Quantum.IQSharp.ExecutionPathTracer;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.Simulation.Simulators;
using System.Linq;
using System.Numerics;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    internal class DebugStateDumper : QuantumSimulator.StateDumper
    {
        private IDictionary<BigInteger, Complex>? _data = null;

        public DebugStateDumper(QuantumSimulator qsim) : base(qsim)
        {
        }

        public override bool Callback([MarshalAs(UnmanagedType.LPStr)] string idx, double real, double img)
        {
            if (_data == null) throw new Exception("Expected data buffer to be initialized before callback, but it was null.");
            _data[CommonNativeSimulator.DisplayableState.BasisStateLabelToBigInt(idx)] = new Complex(real, img);
            return true;
        }
        
        public IDictionary<BigInteger, Complex> GetAmplitudes()
        {
            var count = this.Simulator.QubitManager?.AllocatedQubitsCount ?? 0;
            _data = new Dictionary<BigInteger, Complex>();
            _ = base.Dump();
            return _data;
        }
    }

    /// <summary>
    ///     A magic command that can be used to step through the execution of a
    ///     quantum operation using the full-state simulator.
    /// </summary>
    public class DebugMagic : AbstractMagic
    {
        /// <summary>
        ///     Constructs a new magic command given a resolver used to find
        ///     operations and functions, a configuration source used to set
        ///     configuration options, and a shell router and shell server for
        ///     communication with the client.
        /// </summary>
        public DebugMagic(
                ISymbolResolver resolver, IConfigurationSource configurationSource, IShellRouter router, IShellServer shellServer,
                ILogger<DebugMagic>? logger
        ) : base(
            "debug",
            new Microsoft.Jupyter.Core.Documentation
            {
                Summary = "Steps through the execution of a given Q# operation or function.",
                Description = $@"
                    This magic command allows for stepping through the execution of a given Q# operation
                    or function using the QuantumSimulator.

                    #### Required parameters

                    - Q# operation or function name. This must be the first parameter, and must be a valid Q# operation
                    or function name that has been defined either in the notebook or in a Q# file in the same folder.
                    - Arguments for the Q# operation or function must also be specified as `key=value` pairs.
                ".Dedent(),
                Examples = new[]
                {
                    @"
                        Step through the execution of a Q# operation defined as `operation MyOperation() : Result`:
                        ```
                        In []: %debug MyOperation
                        Out[]: <interactive HTML for stepping through the operation>
                        ```
                    ".Dedent(),
                    @"
                        Step through the execution of a Q# operation defined as `operation MyOperation(a : Int, b : Int) : Result`:
                        ```
                        In []: %debug MyOperation a=5 b=10
                        Out[]: <interactive HTML for stepping through the operation>
                        ```
                    ".Dedent(),
                }
            }, logger)
        {
            this.SymbolResolver = resolver;
            this.ConfigurationSource = configurationSource;
            this.ShellServer = shellServer;
            this.Logger = logger;
            router.RegisterHandler("iqsharp_debug_advance", this.HandleAdvanceMessage);
        }

        private const string ParameterNameOperationName = "__operationName__";
        private readonly ConcurrentDictionary<Guid, ManualResetEvent> SessionAdvanceEvents
            = new ConcurrentDictionary<Guid, ManualResetEvent>();
        private readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        private readonly IShellServer ShellServer;
        private readonly ILogger<DebugMagic>? Logger;

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

        /// <inheritdoc />
        public override ExecutionResult RunCancellable(string input, IChannel channel, CancellationToken token) =>
            RunAsync(input, channel, token).Result;

        internal async Task HandleAdvanceMessage(Message message) => await Task.Run(() =>
        {
            Logger?.LogDebug("Got debug advance message:", message);
            var content = (message.Content as UnknownContent);
            var session = content?.Data?["debug_session"];
            if (session == null)
            {
                Logger?.LogWarning("Got debug advance message, but debug_session was null.", message);
            }
            else
            {
                var sessionGuid = Guid.Parse(session.ToString());
                ManualResetEvent? @event = null;
                lock (SessionAdvanceEvents)
                {
                    @event = SessionAdvanceEvents[sessionGuid];
                }
                @event.Set();
            }
        });

        private async Task WaitForAdvance(Guid session, CancellationToken? token = null) =>
            await Task.Run(() =>
                {
                    ManualResetEvent? @event = null;
                    // Find the event we need to wait on.
                    lock (SessionAdvanceEvents)
                    {
                        @event = SessionAdvanceEvents[session];
                    }
                    @event.Reset();
                    WaitHandle.WaitAny(new[] { @event, (token ?? CancellationTokenSource.Token).WaitHandle });
                },
            token ?? CancellationTokenSource.Token);

        /// <summary>
        ///     Simulates an operation given a string with its name and a JSON
        ///     encoding of its arguments.
        /// </summary>
        public async Task<ExecutionResult> RunAsync(string input, IChannel channel, CancellationToken? token = null)
        {
            var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNameOperationName);

            var name = inputParameters.DecodeParameter<string>(ParameterNameOperationName);

            var symbol = SymbolResolver.Resolve(name) as IQSharpSymbol;
            if (symbol == null) throw new InvalidOperationException($"Invalid operation name: {name}");

            var session = Guid.NewGuid();
            var debugSessionDivId = session.ToString();
            lock (SessionAdvanceEvents)
            {
                SessionAdvanceEvents[session] = new ManualResetEvent(true);
            }

            // Set up the simulator and execution path tracer
            var tracer = new ExecutionPathTracer.ExecutionPathTracer();
            using var qsim = new QuantumSimulator()
                .WithExecutionPathTracer(tracer);

            // Render the placeholder for the debug UX.
            channel.Stdout($"Starting debug session for {name}...");
            channel.Display(new DisplayableHtmlElement($@"<div id='{debugSessionDivId}' />"));

            channel.SendIoPubMessage(
                new Message
                {
                    Header = new MessageHeader
                    {
                        MessageType = "iqsharp_debug_sessionstart"
                    },
                    Content = new DebugSessionContent
                    {
                        DivId = debugSessionDivId,
                        DebugSession = session.ToString(),                        
                    }
                }
            );

            // Render the placeholder for the execution path.
            var executionPathDivId = $"execution-path-container-{Guid.NewGuid()}";
            channel.Display(new DisplayableHtmlElement($"<div id='{executionPathDivId}' />"));

            // Set up the OnOperationStart handler.
            qsim.OnOperationStart += (callable, args) =>
            {
                if (!(token ?? CancellationTokenSource.Token).IsCancellationRequested)
                {
                    var allocatedQubitsCount = (int) (qsim.QubitManager?.AllocatedQubitsCount ?? 0);
                    if (allocatedQubitsCount == 0) return;

                    // Tell the IOPub channel that we're starting a new operation.
                    channel.SendIoPubMessage(
                        new Message
                        {
                            Header = new MessageHeader
                            {
                                MessageType = "iqsharp_debug_opstart"
                            },
                            Content = new DebugStatusContent
                            {
                                DebugSession = session.ToString(),
                                State = new CommonNativeSimulator.DisplayableState
                                {

                                    QubitIds = qsim.QubitIds.Select(q => (int)q),
                                    NQubits = allocatedQubitsCount,
                                    Amplitudes = new DebugStateDumper(qsim).GetAmplitudes(),
                                },
                                Id = debugSessionDivId
                            }
                        }
                    );
                }
                WaitForAdvance(session, token).Wait();
            };

            qsim.OnOperationEnd += (callable, args) =>
            {
                if (!(token ?? CancellationTokenSource.Token).IsCancellationRequested)
                {
                    // Render the `ExecutionPath` traced out by the `ExecutionPathTracer`
                    tracer.RenderExecutionPath(
                        channel,
                        executionPathDivId,
                        renderDepth: 1,
                        this.ConfigurationSource.TraceVisualizationStyle);
                }
            };

            try
            {
                // Simulate the operation.
                var value = await Task.Run(() => symbol.Operation.RunAsync(qsim, inputParameters), token ?? CancellationTokenSource.Token);
                return value.ToExecutionResult();
            }
            finally
            {
                // Report completion.
                channel.Stdout($"Finished debug session for {name}.");
                channel.SendIoPubMessage(
                    new Message
                    {
                        Header = new MessageHeader
                        {
                            MessageType = "iqsharp_debug_sessionend"
                        },
                        Content = new DebugSessionContent
                        {
                            DivId = debugSessionDivId,
                            DebugSession = session.ToString(),                        
                        }
                    }
                );
            }
        }
    }
}
