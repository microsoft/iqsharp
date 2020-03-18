// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp.Jupyter
{

    /// <summary>
    ///     Represents information returned to the client about the current
    ///     kernel instance, such as the current hosting environment.
    /// </summary>
    public class ClientInfoContent : MessageContent
    {
        [JsonProperty("hosting_environment")]
        public string HostingEnvironment { get; set; }

        [JsonProperty("iqsharp_version")]
        public string IQSharpVersion { get; set; } =
            typeof(ClientInfoContent).Assembly.GetName().Version.ToString();
    }

    internal static class MetadataExtensions
    {
        internal static ClientInfoContent AsClientInfoContent(this IMetadataController metadata) =>
            new ClientInfoContent
            {
                HostingEnvironment = metadata.HostingEnvironment
            };
    }

    /// <summary>
    ///     Shell handler that registers new information received from the
    ///     client with an appropriate metadata controller. This allows for
    ///     the client to provide metadata not initially available when the
    ///     kernel starts, such as the browser's user agent string.
    /// </summary>
    public class ClientInfoHandler : IShellHandler
    {
        public string UserAgent { get; private set; }
        private readonly ILogger<EchoHandler> logger;
        private readonly IMetadataController metadata;
        private readonly IShellServer shellServer;
        public ClientInfoHandler(
            ILogger<EchoHandler> logger,
            IMetadataController metadata,
            IShellServer shellServer
        )
        {
            this.logger = logger;
            this.metadata = metadata;
            this.shellServer = shellServer;
        }

        public string MessageType => "iqsharp_clientinfo_request";

        // Note that the order this message occurs in is insignificant, so we
        // can handle the entire message asynchronously.
        public async Task? HandleAsync(Message message)
        {
            metadata.UserAgent = (message.Content as UnknownContent).Data["user_agent"] as string;
            shellServer.SendShellMessage(
                new Message
                {
                    Header = new MessageHeader
                    {
                        MessageType = "iqsharp_clientinfo_reply"
                    },
                    Content = metadata.AsClientInfoContent()
                }
                .AsReplyTo(message)
            );
        }
    }
}
