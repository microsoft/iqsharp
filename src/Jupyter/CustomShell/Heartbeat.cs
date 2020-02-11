// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp.Jupyter
{

    public class HeartbeatReplyContent : MessageContent
    {
        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class HeartbeatHandler : ICustomShellHandler
    {
        private readonly IShellServer shellServer;
        private readonly ILogger<HeartbeatHandler> logger;
        public HeartbeatHandler(
            ILogger<HeartbeatHandler> logger,
            IShellServer shellServer
        )
        {
            this.logger = logger;
            this.shellServer = shellServer;
        }

        public string MessageType => "iqsharp_heartbeat_request";

        public void Handle(Message message)
        {
            // Find out the thing we need to echo back.
            var value = (message.Content as UnknownContent).Data["value"] as string;
            shellServer.SendIoPubMessage(
                new Message
                {
                    Header = new MessageHeader
                    {
                        MessageType = "iqsharp_heartbeat_output"
                    },
                    Content = new HeartbeatReplyContent
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
                        MessageType = "iqsharp_heartbeat_reply"
                    },
                    Content = new HeartbeatReplyContent
                    {
                        Value = value
                    }
                }.AsReplyTo(message)
            );
        }
    }
}
