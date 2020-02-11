// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#nullable enable
using System;
using Microsoft.Extensions.Options;

namespace Microsoft.Quantum.IQSharp
{

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

        public event Action MetadataChanged;
    }

    public class MetadataController : IMetadataController
    {
        private string? userAgent = null;
        private string? hostingEnvironment = null;

        public string? UserAgent
        {
            get => userAgent;
            set
            {
                userAgent = value;
                MetadataChanged?.Invoke();
            }
        }
        public string? HostingEnvironment
        {
            get => hostingEnvironment;
            set
            {
                hostingEnvironment = value;
                MetadataChanged?.Invoke();
            }
        }

        public event Action? MetadataChanged;

        public MetadataController(
            IOptions<ClientInformation> options
        )
        {
            userAgent = options.Value.UserAgent;
            hostingEnvironment = options.Value.HostingEnvironment;
        }
    }

}
