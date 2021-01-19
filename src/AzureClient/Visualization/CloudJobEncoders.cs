// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Azure.Quantum;
using Microsoft.Jupyter.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal static class CloudJobExtensions
    {
        private static DateTime? ToDateTime(this string serializedDateTime) =>
            DateTime.TryParse(serializedDateTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime)
            ? dateTime
            : null as DateTime?;

        // Overloading `ToDateTime` in order to unblock changes in PR https://github.com/microsoft/qsharp-runtime/pull/467. 
        private static DateTime? ToDateTime(this DateTime? dateTime) => dateTime;

        internal static Dictionary<string, object?> ToDictionary(this CloudJob cloudJob) =>
            new Dictionary<string, object?>()
            {
                ["id"] = cloudJob.Id,
                ["name"] = cloudJob.Details.Name,
                ["status"] = cloudJob.Status,
                ["uri"] = cloudJob.Uri.ToString(),
                ["provider"] = cloudJob.Details.ProviderId,
                ["target"] = cloudJob.Details.Target,
                ["creation_time"] = cloudJob.Details.CreationTime.ToDateTime()?.ToUniversalTime(),
                ["begin_execution_time"] = cloudJob.Details.BeginExecutionTime.ToDateTime()?.ToUniversalTime(),
                ["end_execution_time"] = cloudJob.Details.EndExecutionTime.ToDateTime()?.ToUniversalTime(),
            };

        internal static Table<CloudJob> ToJupyterTable(this IEnumerable<CloudJob> jobsList) =>
            new Table<CloudJob>
            {
                Columns = new List<(string, Func<CloudJob, string>)>
                {
                    ("Job Name", cloudJob => cloudJob.Details.Name),
                    ("Job ID", cloudJob => $"<a href=\"{cloudJob.Uri}\" target=\"_blank\">{cloudJob.Id}</a>"),
                    ("Job Status", cloudJob => cloudJob.Status),
                    ("Target", cloudJob => cloudJob.Details.Target),
                    ("Creation Time", cloudJob => cloudJob.Details.CreationTime.ToDateTime()?.ToString() ?? string.Empty),
                    ("Begin Execution Time", cloudJob => cloudJob.Details.BeginExecutionTime.ToDateTime()?.ToString() ?? string.Empty),
                    ("End Execution Time", cloudJob => cloudJob.Details.EndExecutionTime.ToDateTime()?.ToString() ?? string.Empty),
                },
                Rows = jobsList.OrderByDescending(job => job.Details.CreationTime).ToList(),
            };
    }

    /// <summary>
    /// Encodes a <see cref="CloudJob"/> object as HTML.
    /// </summary>
    public class CloudJobToHtmlEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToHtmlDisplayEncoder();

        /// <inheritdoc/>
        public string MimeType => MimeTypes.Html;

        /// <inheritdoc/>
        public EncodedData? Encode(object displayable) =>
            displayable.AsEnumerableOf<CloudJob>() is IEnumerable<CloudJob> jobs
                ? tableEncoder.Encode(jobs.ToJupyterTable())
                : null;
    }

    /// <summary>
    /// Encodes a <see cref="CloudJob"/> object as plain text.
    /// </summary>
    public class CloudJobToTextEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToTextDisplayEncoder();

        /// <inheritdoc/>
        public string MimeType => MimeTypes.PlainText;

        /// <inheritdoc/>
        public EncodedData? Encode(object displayable) =>
            displayable.AsEnumerableOf<CloudJob>() is IEnumerable<CloudJob> jobs
                ? tableEncoder.Encode(jobs.ToJupyterTable())
                : null;
    }

    /// <summary>
    /// Encodes a <see cref="CloudJob"/> object as JSON.
    /// </summary>
    public class CloudJobJsonConverter : JsonConverter<CloudJob>
    {
        /// <inheritdoc/>
        public override CloudJob ReadJson(JsonReader reader, Type objectType, CloudJob? existingValue, bool hasExistingValue, JsonSerializer serializer)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, CloudJob? value, JsonSerializer serializer)
        {
            if (value != null) JToken.FromObject(value.ToDictionary()).WriteTo(writer);
        }
    }

    /// <summary>
    /// Encodes an enumeration of <see cref="CloudJob"/> objects as JSON.
    /// </summary>
    public class CloudJobListJsonConverter : JsonConverter<IEnumerable<CloudJob>>
    {
        /// <inheritdoc/>
        public override IEnumerable<CloudJob> ReadJson(JsonReader reader, Type objectType, IEnumerable<CloudJob>? existingValue, bool hasExistingValue, JsonSerializer serializer)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, IEnumerable<CloudJob>? value, JsonSerializer serializer)
        {
            if (value != null) JToken.FromObject(value.Select(job => job.ToDictionary())).WriteTo(writer);
        }
    }
}
