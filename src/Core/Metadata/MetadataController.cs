// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#nullable enable
using System;
using Microsoft.Extensions.Options;

namespace Microsoft.Quantum.IQSharp
{

    /// <summary>
    ///     Raised when the available metadata has changed (e.g.: the client
    ///     sends its user agent to the kernel).
    /// </summary>
    /// <param name="propertyName">
    ///     Name of the metadata property whose value changed. May possibly
    ///     be <c>null</c> if the property name is not known.
    /// </param>
    public delegate void OnMetadataChanged(IMetadataController sender, string? propertyName);

    /// <summary>
    ///      Service that controls client-side metadata, whether that metadata
    ///      comes via initial configuration (e.g. environment variables,
    ///      command-line arguments, or JSON config files), or from runtime
    ///      communication with the client.
    /// </summary>
    public interface IMetadataController
    {
        /// <summary>
        ///      A string passed by the client representing the client name.
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        ///      A string passed by the client representing the stable client id.
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        ///      A boolean passed by the client representing the whether the client id is new.
        /// </summary>
        public bool? ClientIsNew { get; set; }

        /// <summary>
        ///      A string passed by the client representing the client's country.
        ///      The value can come from a ReverseIP lookup, and is in the
        ///      <see href="https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2">ISO 3166-1 alpha-2</see>
        ///      format ("US", for example).
        /// </summary>
        public string? ClientCountry { get; set; }

        /// <summary>
        ///      A string passed by the client representing the client's language.
        ///      The value can come from the browser's <see href="https://developer.mozilla.org/en-US/docs/Web/API/NavigatorLanguage/language">navigator.language</see>
        ///      in the <see href="https://tools.ietf.org/rfc/bcp/bcp47.txt">BCP 47</see> format ("en-US", for example).
        /// </summary>
        public string? ClientLanguage { get; set; }

        /// <summary>
        ///      A string passed by the client representing the client's hostname.
        ///      The value can come from the browser's <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location">location.hostname</see>
        ///      and containts the domain of the URL hosting the Jupyter Notebook (for example "localhost" or "mybinder.org").
        /// </summary>
        public string? ClientHost { get; set; }

        /// <summary>
        ///      A string passed via the client query string 'origin' identifying the source of the traffic.
        ///      This is used to track links that directed the user to IQ# hosted in the cloud.
        /// </summary>
        public string? ClientOrigin { get; set; }

        /// <summary>
        ///      The first origin ever for the user (see ClientOrigin).
        ///      This is used to track which link that directed the user to IQ# hosted in the cloud for the first time.
        /// </summary>
        public string? ClientFirstOrigin { get; set; }

        /// <summary>
        ///     A string passed by the client representing the environment in which
        ///     the client is running (e.g.: continuous integration, a hosted
        ///     notebook service, etc.).
        /// </summary>
        public string? HostingEnvironment { get; set; }

        /// <summary>
        ///     IQ# version based on the assembly's version
        /// </summary>
        public string? IQSharpVersion { get; }

        /// <summary>
        ///     Raised when the available metadata has changed (e.g.: the client
        ///     sends its user agent to the kernel).
        /// </summary>
        public event OnMetadataChanged? MetadataChanged;

        /// <summary>
        ///     Environment variable that is set to turn off the telemetry
        /// </summary>
        public bool? TelemetryOptOut { get; set; }
    }

    public class MetadataController : IMetadataController
    {
        private string? userAgent = null;
        private string? hostingEnvironment = null;
        private string? clientId = null;
        private bool? clientIsNew = null;
        private string? clientCountry = null;
        private string? clientLanguage = null;
        private string? clientHost = null;
        private string? clientOrigin = null;
        private string? clientFirstOrigin = null;
        private bool? telemetryOptOut = null;

        public string? IQSharpVersion { get; }
        public string? UserAgent
        {
            get => userAgent;
            set => SetPropertyAndNotifyChange(ref userAgent, value);
        }
        public string? HostingEnvironment
        {
            get => hostingEnvironment;
            set => SetPropertyAndNotifyChange(ref hostingEnvironment, value);
        }
        public string? ClientId
        {
            get => clientId;
            set => SetPropertyAndNotifyChange(ref clientId, value);
        }
        public bool? ClientIsNew
        {
            get => clientIsNew;
            set => SetPropertyAndNotifyChange(ref clientIsNew, value);
        }
        public bool? TelemetryOptOut
        {
            get => telemetryOptOut;
            set => SetPropertyAndNotifyChange(ref telemetryOptOut, value);
        }
        public string? ClientCountry
        {
            get => clientCountry;
            set => SetPropertyAndNotifyChange(ref clientCountry, value);
        }
        public string? ClientLanguage
        {
            get => clientLanguage;
            set => SetPropertyAndNotifyChange(ref clientLanguage, value);
        }
        public string? ClientHost
        {
            get => clientHost;
            set => SetPropertyAndNotifyChange(ref clientHost, value);
        }
        public string? ClientOrigin
        {
            get => clientOrigin;
            set => SetPropertyAndNotifyChange(ref clientOrigin, value);
        }
        public string? ClientFirstOrigin
        {
            get => clientFirstOrigin;
            set => SetPropertyAndNotifyChange(ref clientFirstOrigin, value);
        }

        protected void SetPropertyAndNotifyChange<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            if (!object.Equals(field, value))
            {
                field = value;
                MetadataChanged?.Invoke(this, propertyName);
            }
        }

        public event OnMetadataChanged? MetadataChanged;

        public MetadataController(
            // We take an options instance with the client information initially
            // available when the service collection is first constructed (e.g.:
            // through environment variables set at kernel start).
            IOptions<ClientInformation> clientInformation,
            IEventService eventService
        )
        {
            UserAgent =
                clientInformation.Value.UserAgent
                + (string.IsNullOrWhiteSpace(clientInformation.Value.UserAgentExtra) ? "" : $"({clientInformation.Value.UserAgentExtra})");
            HostingEnvironment = clientInformation.Value.HostingEnvironment;
            TelemetryOptOut = clientInformation.Value.IsTelemetryOptOut;
            IQSharpVersion = typeof(MetadataController).Assembly.GetName().Version.ToString();
            eventService?.TriggerServiceInitialized<IMetadataController>(this);
        }
    }

}
