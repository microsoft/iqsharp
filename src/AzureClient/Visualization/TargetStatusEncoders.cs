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
    internal static class TargetStatusExtensions
    {
        internal static Dictionary<string, object> ToDictionary(this TargetStatus target) =>
            new Dictionary<string, object>()
            {
                { "targetName", target.Id },
                { "currentAvailability", target.CurrentAvailability },
                { "averageQueueTime", target.AverageQueueTime },
                { "statusPage", target.StatusPage },
            };

        internal static Table<TargetStatus> ToJupyterTable(this IEnumerable<TargetStatus> targets) =>
            new Table<TargetStatus>
            {
                Columns = new List<(string, Func<TargetStatus, string>)>
                    {
                        ("Target Name", target => target.Id),
                        ("Current Availability", target => target.CurrentAvailability),
                        ("Average Queue Time", target => target.AverageQueueTime.ToString()),
                        ("Status Page", target => target.StatusPage),
                    },
                Rows = targets.ToList()
            };
    }

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
