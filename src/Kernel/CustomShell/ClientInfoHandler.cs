// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp.Kernel
{

    /// <summary>
    ///     Represents information returned to the client about the current
    ///     kernel instance, such as the current hosting environment.
    /// </summary>
    internal class ClientInfoContent : MessageContent
    {
        [JsonProperty("hosting_environment")]
        public string HostingEnvironment { get; set; }

        [JsonProperty("iqsharp_version")]
        public string IQSharpVersion { get; set; }

        [JsonProperty("user_agent")]
        public string UserAgent { get; set; }

        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("client_isnew")]
        public bool? ClientIsNew { get; set; }

        [JsonProperty("client_host")]
        public string ClientHost { get; set; }

        [JsonProperty("client_origin")]
        public string ClientOrigin { get; set; }

        [JsonProperty("client_first_origin")]
        public string ClientFirstOrigin { get; set; }

        [JsonProperty("client_language")]
        public string ClientLanguage { get; set; }

        [JsonProperty("client_country")]
        public string ClientCountry { get; set; }

        [JsonProperty("telemetry_opt_out")]
        public bool? TelemetryOptOut { get; set; }
    }

    internal static class MetadataExtensions
    {
        internal static ClientInfoContent AsClientInfoContent(this IMetadataController metadata) =>
            new ClientInfoContent
            {
                HostingEnvironment = metadata.HostingEnvironment,
                IQSharpVersion = metadata.IQSharpVersion,
                TelemetryOptOut = metadata.TelemetryOptOut
            };
    }

    /// <summary>
    ///     Shell handler that registers new information received from the
    ///     client with an appropriate metadata controller. This allows for
    ///     the client to provide metadata not initially available when the
    ///     kernel starts, such as the browser's user agent string.
    /// </summary>
    internal class ClientInfoHandler : IShellHandler
    {
        public string UserAgent { get; private set; }
        private readonly ILogger<ClientInfoHandler> logger;
        private readonly IMetadataController metadata;
        private readonly IShellServer shellServer;
        public ClientInfoHandler(
            ILogger<ClientInfoHandler> logger,
            IMetadataController metadata,
            IShellServer shellServer
        )
        {
            this.logger = logger;
            this.metadata = metadata;
            this.shellServer = shellServer;
        }

        /// <inheritdoc />
        public string MessageType => "iqsharp_clientinfo_request";

        /// <inheritdoc />
        public async Task HandleAsync(Message message)
        {
            var content = message.To<ClientInfoContent>();
            metadata.UserAgent = content.UserAgent ?? metadata.UserAgent;
            metadata.ClientId = content.ClientId ?? metadata.ClientId;
            metadata.ClientIsNew = content.ClientIsNew ?? metadata.ClientIsNew;
            metadata.ClientCountry = content.ClientCountry ?? metadata.ClientCountry;
            metadata.ClientLanguage = content.ClientLanguage ?? metadata.ClientLanguage;
            metadata.ClientHost = content.ClientHost ?? metadata.ClientHost;
            metadata.ClientOrigin = content.ClientOrigin ?? metadata.ClientOrigin;
            metadata.ClientFirstOrigin = content.ClientFirstOrigin ?? metadata.ClientFirstOrigin;
            await Task.Run(() => shellServer.SendShellMessage(
                new Message
                {
                    Header = new MessageHeader
                    {
                        MessageType = "iqsharp_clientinfo_reply"
                    },
                    Content = metadata.AsClientInfoContent()
                }
                .AsReplyTo(message)
            ));
        }
    }
}
