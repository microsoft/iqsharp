// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp.Kernel
{

    internal class EchoReplyContent : MessageContent
    {
        [JsonProperty("value")]
        public string Value { get; set; }
    }

    /// <summary>
    ///     Allows clients to send "echo" messages to test shell and iopub
    ///     communications with the kernel.
    /// </summary>
    public class EchoHandler : IShellHandler
    {
        private readonly IShellServer shellServer;
        private readonly ILogger<EchoHandler> logger;

        /// <summary>
        ///     Creates an <see cref="EchoHandler"/> object.
        /// </summary>
        public EchoHandler(
            ILogger<EchoHandler> logger,
            IShellServer shellServer
        )
        {
            this.logger = logger;
            this.shellServer = shellServer;
        }

        /// <inheritdoc/>
        public string MessageType => "iqsharp_echo_request";

        /// <inheritdoc/>
        public async Task HandleAsync(Message message)
        {
            // Find out the thing we need to echo back.
            var value = (message.Content as UnknownContent).Data["value"] as string;
            // Send the echo both as an output and as a reply so that clients
            // can test both kinds of callbacks.
            await Task.Run(() =>
            {
                shellServer.SendIoPubMessage(
                    new Message
                    {
                        Header = new MessageHeader
                        {
                            MessageType = "iqsharp_echo_output"
                        },
                        Content = new EchoReplyContent
                        {
                            Value = value
                        }
                    }.AsReplyTo(message)
                );
                shellServer.SendShellMessage(
                    new Message
                    {
                        Header = new MessageHeader
                        {
                            MessageType = "iqsharp_echo_reply"
                        },
                        Content = new EchoReplyContent
                        {
                            Value = value
                        }
                    }.AsReplyTo(message)
                );
            });
        }
    }
}
