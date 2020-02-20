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
    public delegate void OnMetadataChanged(string? propertyName);

    /// <summary>
    ///      Service that controls client-side metadata, whether that metadata
    ///      comes via initial configuration (e.g. environment variables,
    ///      command-line arguments, or JSON config files), or from runtime
    ///      communication with the client.
    /// </summary>
    public interface IMetadataController
    {
        /// <summary>
        ///      A string passed by the client representing the name of the client.
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        ///     A string passed by the client representing the environment in which
        ///     the client is running (e.g.: continuous integration, a hosted
        ///     notebook service, etc.).
        /// </summary>
        public string? HostingEnvironment { get; set; }


        /// <summary>
        ///     Raised when the available metadata has changed (e.g.: the client
        ///     sends its user agent to the kernel).
        /// </summary>
        public event OnMetadataChanged? MetadataChanged;
    }

    public class MetadataController : IMetadataController
    {
        private string? userAgent = null;
        private string? hostingEnvironment = null;

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

        protected void SetPropertyAndNotifyChange<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            if (!object.Equals(field, value))
            {
                field = value;
                MetadataChanged?.Invoke(propertyName);
            }
        }

        public event OnMetadataChanged? MetadataChanged;

        public MetadataController(
            // We take an options instance with the client information initially
            // available when the service collection is first constructed (e.g.:
            // through environment variables set at kernel start).
            IOptions<ClientInformation> options
        )
        {
            UserAgent = options.Value.UserAgent;
            HostingEnvironment = options.Value.HostingEnvironment;
        }
    }

}
