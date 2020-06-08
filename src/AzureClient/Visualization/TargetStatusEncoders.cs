// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Azure.Quantum.Client.Models;
using Microsoft.Jupyter.Core;
using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    public class TargetStatusToHtmlEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToHtmlDisplayEncoder();

        public string MimeType => MimeTypes.Html;

        public EncodedData? Encode(object displayable)
        {
            if (displayable is TargetStatus target) displayable = new List<TargetStatus>() { target };

            return displayable is IEnumerable<TargetStatus> targets
                ? tableEncoder.Encode(targets.ToJupyterTable())
                : null;
        }
    }

    public class TargetStatusToTextEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToTextDisplayEncoder();

        public string MimeType => MimeTypes.PlainText;

        public EncodedData? Encode(object displayable)
        {
            if (displayable is TargetStatus target) displayable = new List<TargetStatus>() { target };

            return displayable is IEnumerable<TargetStatus> targets
                ? tableEncoder.Encode(targets.ToJupyterTable())
                : null;
        }
    }

    public class TargetStatusToJsonEncoder : IResultEncoder
    {
        public string MimeType => MimeTypes.Json;

        public EncodedData? Encode(object displayable)
        {
            if (displayable is TargetStatus target) displayable = new List<TargetStatus>() { target };

            if (displayable is IEnumerable<TargetStatus> targets)
            {
                var serialized = JsonConvert.SerializeObject(targets.Select(target => target.ToDictionary()));
                return serialized.ToEncodedData();
            }
            else return null;
        }
    }
}
