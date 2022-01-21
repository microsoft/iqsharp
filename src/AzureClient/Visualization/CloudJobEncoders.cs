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
                ["name"] = cloudJob.Details.Name ?? string.Empty,
                ["status"] = cloudJob.Status ?? string.Empty,
                ["uri"] = cloudJob.Uri.ToString(),
                ["provider"] = cloudJob.Details.ProviderId ?? string.Empty,
                ["target"] = cloudJob.Details.Target ?? string.Empty,
                ["creation_time"] = cloudJob.Details.CreationTime?.ToUniversalTime(),
                ["begin_execution_time"] = cloudJob.Details.BeginExecutionTime?.ToUniversalTime(),
                ["end_execution_time"] = cloudJob.Details.EndExecutionTime?.ToUniversalTime(),
                ["cost_estimate"] = cloudJob.GetCostEstimateText(),
            };

        internal static Table<CloudJob> ToJupyterTable(this IEnumerable<CloudJob> jobsList) =>
            new Table<CloudJob>
            {
                Columns = new List<(string, Func<CloudJob, string>)>
                {
                    ("Job Name", cloudJob => cloudJob.Details.Name ?? cloudJob.Id),
                    ("Job ID", cloudJob => $"<a href=\"{cloudJob.Uri}\" target=\"_blank\">{cloudJob.Id}</a>"),
                    ("Job Status", cloudJob => cloudJob.Status ?? string.Empty),
                    ("Target", cloudJob => cloudJob.Details.Target ?? string.Empty),
                    ("Creation Time", cloudJob => cloudJob.Details.CreationTime?.ToString() ?? string.Empty),
                    ("Begin Execution Time", cloudJob => cloudJob.Details.BeginExecutionTime?.ToString() ?? string.Empty),
                    ("End Execution Time", cloudJob => cloudJob.Details.EndExecutionTime?.ToString() ?? string.Empty),
                    ("Cost Estimate", cloudJob => cloudJob.GetCostEstimateText()),
                },
                Rows = jobsList.OrderByDescending(job => job.Details.CreationTime).ToList(),
            };

        internal static string GetCostEstimateText(this CloudJob cloudJob) =>
            cloudJob?.Details?.CostEstimate == null
            ? String.Empty
            : CurrencyHelper.FormatValue(cloudJob.Details.CostEstimate?.CurrencyCode,
                                         cloudJob.Details.CostEstimate?.EstimatedTotal);
    }

    internal static class CurrencyHelper
    {
        private static Dictionary<string, CultureInfo> currencyCodeToCultureInfo = new Dictionary<string, CultureInfo>();
        static CurrencyHelper()
        {
            foreach (var cultureInfo in CultureInfo.GetCultures(CultureTypes.AllCultures)
                                                   .Where((c) => !string.IsNullOrEmpty(c.Name) && !c.IsNeutralCulture))
            {
                try
                {
                    var regionInfo = new RegionInfo(cultureInfo.Name);
                    currencyCodeToCultureInfo.TryAdd(regionInfo.ISOCurrencySymbol, cultureInfo);
                }
                catch {}
            }
        }

        public static CultureInfo? GetCultureInfoForCurrencyCode(string currencyCode)
        {
            if (currencyCodeToCultureInfo.TryGetValue(currencyCode, out var cultureInfo))
            {
                return cultureInfo;
            }

            return null;
        }

        public static string FormatValue(string? currencyCode, float? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(currencyCode))
            {
                return $"{value:F2}";
            }

            if (currencyCodeToCultureInfo.TryGetValue(currencyCode, out var cultureInfo))
            {
                return value?.ToString("C", cultureInfo) ?? string.Empty;
            }

            return $"{currencyCode} {value:F2}";
        }
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
