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
        /// </summary>
        public string? ClientCountry { get; set; }

        /// <summary>
        ///      A string passed by the client representing the client's language.
        /// </summary>
        public string? ClientLanguage { get; set; }

        /// <summary>
        ///      A string passed by the client representing the client's host.
        /// </summary>
        public string? ClientHost { get; set; }

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
            IEventService eventService,
            ITelemetryService _
        )
        {
            UserAgent = clientInformation.Value.UserAgent;
            HostingEnvironment = clientInformation.Value.HostingEnvironment;
            TelemetryOptOut = clientInformation.Value.IsTelemetryOptOut;
            IQSharpVersion = typeof(MetadataController).Assembly.GetName().Version.ToString();
            eventService?.TriggerServiceInitialized<IMetadataController>(this);
        }
    }

}
