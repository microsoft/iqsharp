// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Azure.Quantum;
using Microsoft.Identity.Client;
using Microsoft.Jupyter.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    /// Encodes a <see cref="DeviceCodeResult"/> object as HTML.
    /// </summary>
    public class DeviceCodeResultToHtmlEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToHtmlDisplayEncoder();

        /// <inheritdoc/>
        public string MimeType => MimeTypes.Html;

        /// <inheritdoc/>
        public EncodedData? Encode(object displayable)
        {
            if (displayable is DeviceCodeResult deviceCode)
            {
                var guid = Guid.NewGuid();
                var htmlMessage = deviceCode.Message
                    .Replace(deviceCode.VerificationUrl, $"<a href=\"{deviceCode.VerificationUrl}\"><code>{deviceCode.VerificationUrl}</code></a>")
                    .Replace(
                        deviceCode.UserCode,
                        $@"<span id=""{guid}"" style=""background-color: #e0e0e0;"">
                            <i class=""fa fa-clipboard"" aria-hidden=""true""></i>
                            <strong style=""padding-right: 0.2em"">{deviceCode.UserCode.Trim()}</strong></span>"
                    );
                var attach = $@"<script>
                    window.iqsharp.addCopyListener(""{guid}"", ""{deviceCode.UserCode}"");
                </script>";
                return (htmlMessage + "\n" + attach).ToEncodedData();
            } else return null;
        }
    }

    /// <summary>
    /// Encodes a <see cref="DeviceCodeResult"/> object as plain text.
    /// </summary>
    public class DeviceCodeResultToTextEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToHtmlDisplayEncoder();

        /// <inheritdoc/>
        public string MimeType => MimeTypes.PlainText;

        /// <inheritdoc/>
        public EncodedData? Encode(object displayable) =>
            displayable is DeviceCodeResult deviceCode
                ? deviceCode.Message.ToEncodedData()
                : null as EncodedData?;
    }
}
