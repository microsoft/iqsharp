// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;
using System.Linq;
using System.Numerics;


namespace Microsoft.Quantum.IQSharp.Kernel
{
    public class RawHtmlPayload
    {
        public string Value { get; set; }
    }

    public class RawHtmlEncoder : IResultEncoder
    {
        public string MimeType => MimeTypes.Html;

        public EncodedData? Encode(object displayable) =>
            displayable is RawHtmlPayload payload
            ? payload.Value.ToEncodedData()
            : (Nullable<EncodedData>)null;
    }

        public class DebugStateDumper: QuantumSimulator.StateDumper
        {
            public DebugStateDumper (QuantumSimulator qsim) : base(qsim)
            {

            }

            public override bool Callback(uint idx, double real, double img)
            {
                if (_data == null) throw new Exception("Expected data buffer to be initialized before callback, but it was null.");
                _data[idx] = new Complex(real, img);
                return true;
            }
            private Complex[]? _data = null;
            public Complex[] GetAmplitudes()
            {
                var count = this.Simulator.QubitManager.GetAllocatedQubitsCount();
                _data = new Complex[1 << ((int)count)];
                var result = base.Dump();
                    return _data;
            }
        }

    public class DebugMagic : AbstractMagic
    {
        public DebugMagic(
                ISymbolResolver resolver, IConfigurationSource configurationSource, IShellRouter router, IShellServer shellServer,
                ILogger<DebugMagic> logger
        ) : base(
            "debug",
            new Documentation
            {
                Summary = "TODO"
            })
        {
            this.SymbolResolver = resolver;
            this.ConfigurationSource = configurationSource;
            this.shellServer = shellServer;
            this.logger = logger;
            router.RegisterHandler("iqsharp_debug_request", this.HandleStartMessage);
            router.RegisterHandler("iqsharp_debug_advance", this.HandleAdvanceMessage);
        }

        private const string ParameterNameOperationName = "__operationName__";
        private ConcurrentDictionary<Guid, ManualResetEvent> sessionAdvanceEvents
            = new ConcurrentDictionary<Guid, ManualResetEvent>();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private IShellServer shellServer;
        private ILogger<DebugMagic> logger;

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

        private async Task HandleStartMessage(Message message) => await Task.Run(() =>
        {
            var content = (message.Content as UnknownContent);
            var session = content?.Data?["debug_session"];
            if (session == null)
            {
                // TODO: Communicate error back here!
            }
            else
            {
                shellServer.SendShellMessage(
                    new Message
                    {
                        Header = new MessageHeader
                        {
                            MessageType = "iqsharp_debug_reply"
                        },
                        Content = new UnknownContent
                        {
                            Data = new Dictionary<string, object>
                            {
                                ["debug_session"] = session
                            }
                        }
                    }
                    .AsReplyTo(message)
                );
            }
        });

        private async Task HandleAdvanceMessage(Message message) => await Task.Run(() =>
        {
            logger.LogDebug("Got debug advance message:", message);
            var content = (message.Content as UnknownContent);
            var session = content?.Data?["debug_session"];
            if (session == null)
            {
                logger.LogWarning("Got debug advance message, but debug_session was null.", message);
            }
            else
            {
                var sessionGuid = Guid.Parse(session.ToString());
                ManualResetEvent? @event = null;
                lock (sessionAdvanceEvents)
                {
                    @event = sessionAdvanceEvents[sessionGuid];
                }
                @event.Set();
            }
        });

        private async Task WaitForAdvance(Guid session)
        {
            await Task.Run(() =>
            {
                ManualResetEvent? @event = null;
                // Find the event we need to wait on.
                lock (sessionAdvanceEvents)
                {
                    @event = sessionAdvanceEvents[session];
                }
                @event.Reset();
                @event.WaitOne();
            }, cancellationTokenSource.Token);
        }

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

            var session = Guid.NewGuid();
            var divId = session.ToString();
            lock (sessionAdvanceEvents)
            {
                sessionAdvanceEvents[session] = new ManualResetEvent(true);
            }

            using var qsim = new QuantumSimulator();
            qsim.OnOperationStart += (callable, args) =>
            {
                // Tell the IOPub channel that we're starting a new operation.
                shellServer.SendIoPubMessage(
                    new Message
                    {
                        Header = new MessageHeader
                        {
                            MessageType = "iqsharp_debug_opstart"
                        },
                        Content = new DebugStatusContent
                        {
                            DebugSession = session.ToString(),
                            State = new DisplayableState
                            {

                                QubitIds = qsim.QubitIds.Select(q => (int)q),
                                NQubits = (int) (qsim.QubitManager?.GetAllocatedQubitsCount() ?? 0),
                                Amplitudes = new DebugStateDumper(qsim).getAmplitudes(),
                                DivId = divId
                            }
                        }
                    }
                );
                WaitForAdvance(session).Wait();
            };

            channel.Display(new RawHtmlPayload
            {
                //needs to be changed?
                Value = $@"
                    <div id=""{divId}""></div>
                "
            });

            shellServer.SendIoPubMessage(
                new Message
                {
                    Header = new MessageHeader
                    {
                        MessageType = "iqsharp_debug_sessionstart"
                    },
                    Content = new DebugSessionContent
                    {
                        DivId = divId,
                        DebugSession = session.ToString(),                        
                    }
                }
            );

            var value = await Task.Run(() => symbol.Operation.RunAsync(qsim, inputParameters));
            return value.ToExecutionResult();
        }
    }
}
