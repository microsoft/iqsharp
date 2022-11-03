// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp.Kernel
{

    /// <summary>
    ///     Represents information returned to the client about the current
    ///     kernel instance, such as the current hosting environment.
    /// </summary>
    internal class ClientInfoContent
    {
        [JsonProperty("hosting_environment")]
        public string? HostingEnvironment { get; set; }

        [JsonProperty("iqsharp_version")]
        public string? IQSharpVersion { get; set; }

        [JsonProperty("user_agent")]
        public string? UserAgent { get; set; }

        [JsonProperty("client_id")]
        public string? ClientId { get; set; }

        [JsonProperty("client_isnew")]
        public bool? ClientIsNew { get; set; }

        [JsonProperty("client_host")]
        public string? ClientHost { get; set; }

        [JsonProperty("client_origin")]
        public string? ClientOrigin { get; set; }

        [JsonProperty("client_first_origin")]
        public string? ClientFirstOrigin { get; set; }

        [JsonProperty("client_language")]
        public string? ClientLanguage { get; set; }

        [JsonProperty("client_country")]
        public string? ClientCountry { get; set; }

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
    ///     Comms service that registers new information received from the
    ///     client with an appropriate metadata controller. This allows for
    ///     the client to provide metadata not initially available when the
    ///     kernel starts, such as the browser's user agent string.
    /// </summary>
    internal class ClientInfoListener
    {
        public string? UserAgent { get; private set; }
        private readonly ILogger<ClientInfoListener> logger;
        private readonly IMetadataController metadata;
        private readonly ICommsRouter commsRouter;
        public ClientInfoListener(
            ILogger<ClientInfoListener> logger,
            IMetadataController metadata,
            ICommsRouter commsRouter
        )
        {
            this.logger = logger;
            this.metadata = metadata;
            this.commsRouter = commsRouter;

            logger.LogDebug("Started client info listener.");
            commsRouter.SessionOpenEvent("iqsharp_clientinfo").On += async (session, data) =>
            {
                logger.LogDebug("Got iqsharp_clientinfo message: {Data}", data.ToString());
                if (!data.TryAs<ClientInfoContent>(out var content))
                {
                    logger.LogError(
                        "Got client info via comms, but failed to deserialize:\n{RawData}",
                        data.ToString()
                    );
                    return;
                }
                metadata.UserAgent = content.UserAgent ?? metadata.UserAgent;
                metadata.ClientId = content.ClientId ?? metadata.ClientId;
                metadata.ClientIsNew = content.ClientIsNew ?? metadata.ClientIsNew;
                metadata.ClientCountry = content.ClientCountry ?? metadata.ClientCountry;
                metadata.ClientLanguage = content.ClientLanguage ?? metadata.ClientLanguage;
                metadata.ClientHost = content.ClientHost ?? metadata.ClientHost;
                metadata.ClientOrigin = content.ClientOrigin ?? metadata.ClientOrigin;
                metadata.ClientFirstOrigin = content.ClientFirstOrigin ?? metadata.ClientFirstOrigin;
                await Task.Run(() =>
                {
                    session.SendMessage(
                        metadata.AsClientInfoContent()
                    );
                    session.Close();
                });
            };
        }
    }
}
