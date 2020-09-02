// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Azure.Quantum.Client.Models;
using Microsoft.Jupyter.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal static class TargetStatusExtensions
    {
        internal static Dictionary<string, object> ToDictionary(this TargetStatus target) =>
            new Dictionary<string, object>()
            {
                ["id"] = target.Id,
                ["current_availability"] = target.CurrentAvailability,
                ["average_queue_time"] = target.AverageQueueTime ?? 0,
            };

        internal static Table<TargetStatus> ToJupyterTable(this IEnumerable<TargetStatus> targets) =>
            new Table<TargetStatus>
            {
                Columns = new List<(string, Func<TargetStatus, string>)>
                {
                    ("Target ID", target => target.Id ?? string.Empty),
                    ("Current Availability", target => target.CurrentAvailability ?? string.Empty),
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
            displayable.AsEnumerableOf<TargetStatus>() is IEnumerable<TargetStatus> targets
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
            displayable.AsEnumerableOf<TargetStatus>() is IEnumerable<TargetStatus> targets
                ? tableEncoder.Encode(targets.ToJupyterTable())
                : null;
    }

    /// <summary>
    /// Encodes a <see cref="TargetStatus"/> object as JSON.
    /// </summary>
    public class TargetStatusJsonConverter : JsonConverter<TargetStatus>
    {
        /// <inheritdoc/>
        public override TargetStatus ReadJson(JsonReader reader, Type objectType, TargetStatus? existingValue, bool hasExistingValue, JsonSerializer serializer)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, TargetStatus? value, JsonSerializer serializer)
        {
            if (value != null) JToken.FromObject(value.ToDictionary()).WriteTo(writer);
        }
    }

    /// <summary>
    /// Encodes an enumeration of <see cref="TargetStatus"/> objects as JSON.
    /// </summary>
    public class TargetStatusListJsonConverter : JsonConverter<IEnumerable<TargetStatus>>
    {
        /// <inheritdoc/>
        public override IEnumerable<TargetStatus> ReadJson(JsonReader reader, Type objectType, IEnumerable<TargetStatus>? existingValue, bool hasExistingValue, JsonSerializer serializer)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, IEnumerable<TargetStatus>? value, JsonSerializer serializer)
        {
            if (value != null) JToken.FromObject(value.Select(job => job.ToDictionary())).WriteTo(writer);
        }
    }
}
