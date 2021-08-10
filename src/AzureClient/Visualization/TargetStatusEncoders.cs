// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Azure.Quantum.Jobs.Models;

using Microsoft.Azure.Quantum;
using Microsoft.Jupyter.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal static class TargetStatusExtensions
    {
        internal static Dictionary<string, object> ToDictionary(this TargetStatusInfo target) =>
            new Dictionary<string, object>()
            {
                ["id"] = target.TargetId ?? string.Empty,
                ["current_availability"] = target.CurrentAvailability ?? string.Empty,
                ["average_queue_time"] = target.AverageQueueTime ?? 0,
            };

        internal static Table<TargetStatusInfo> ToJupyterTable(this IEnumerable<TargetStatusInfo> targets) =>
            new Table<TargetStatusInfo>
            {
                Columns = new List<(string, Func<TargetStatusInfo, string>)>
                {
                    ("Target ID", target => target.TargetId ?? string.Empty),
                    ("Current Availability", target => target.CurrentAvailability?.ToString() ?? string.Empty),
                    ("Average Queue Time (Seconds)", target => target.AverageQueueTime?.ToString() ?? string.Empty),
                },
                Rows = targets.ToList()
            };
    }

    /// <summary>
    /// Encodes a <see cref="TargetStatus"/> object as HTML.
    /// </summary>
    public class TargetStatusToHtmlEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToHtmlDisplayEncoder();

        /// <inheritdoc/>
        public string MimeType => MimeTypes.Html;

        /// <inheritdoc/>
        public EncodedData? Encode(object displayable) =>
            displayable.AsEnumerableOf<TargetStatusInfo>() is IEnumerable<TargetStatusInfo> targets
                ? tableEncoder.Encode(targets.ToJupyterTable())
                : null;
    }

    /// <summary>
    /// Encodes a <see cref="TargetStatus"/> object as plain text.
    /// </summary>
    public class TargetStatusToTextEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToTextDisplayEncoder();

        /// <inheritdoc/>
        public string MimeType => MimeTypes.PlainText;

        /// <inheritdoc/>
        public EncodedData? Encode(object displayable) =>
            displayable.AsEnumerableOf<TargetStatusInfo>() is IEnumerable<TargetStatusInfo> targets
                ? tableEncoder.Encode(targets.ToJupyterTable())
                : null;
    }

    /// <summary>
    /// Encodes a <see cref="TargetStatus"/> object as JSON.
    /// </summary>
    public class TargetStatusJsonConverter : JsonConverter<TargetStatusInfo>
    {
        /// <inheritdoc/>
        public override TargetStatusInfo ReadJson(JsonReader reader, Type objectType, TargetStatusInfo? existingValue, bool hasExistingValue, JsonSerializer serializer)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, TargetStatusInfo? value, JsonSerializer serializer)
        {
            if (value != null) JToken.FromObject(value.ToDictionary()).WriteTo(writer);
        }
    }

    /// <summary>
    /// Encodes an enumeration of <see cref="TargetStatus"/> objects as JSON.
    /// </summary>
    public class TargetStatusListJsonConverter : JsonConverter<IEnumerable<TargetStatusInfo>>
    {
        /// <inheritdoc/>
        public override IEnumerable<TargetStatusInfo> ReadJson(JsonReader reader, Type objectType, IEnumerable<TargetStatusInfo>? existingValue, bool hasExistingValue, JsonSerializer serializer)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, IEnumerable<TargetStatusInfo>? value, JsonSerializer serializer)
        {
            if (value != null) JToken.FromObject(value.Select(job => job.ToDictionary())).WriteTo(writer);
        }
    }
}
