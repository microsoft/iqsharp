// Copyright (c) Microsoft Corporation. All rights reserved.
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

        internal static Dictionary<string, object> ToDictionary(this CloudJob cloudJob) =>
            new Dictionary<string, object>()
            {
                // TODO: add cloudJob.Uri after https://github.com/microsoft/qsharp-runtime/issues/236 is fixed.
                ["id"] = cloudJob.Id,
                ["name"] = cloudJob.Details.Name,
                ["status"] = cloudJob.Status,
                ["provider"] = cloudJob.Details.ProviderId,
                ["target"] = cloudJob.Details.Target,
                ["creationTime"] = cloudJob.Details.CreationTime.ToDateTime(),
                ["beginExecutionTime"] = cloudJob.Details.BeginExecutionTime.ToDateTime(),
                ["endExecutionTime"] = cloudJob.Details.EndExecutionTime.ToDateTime(),
            };

        internal static Table<CloudJob> ToJupyterTable(this IEnumerable<CloudJob> jobsList) =>
            new Table<CloudJob>
            {
                Columns = new List<(string, Func<CloudJob, string>)>
                {
                    // TODO: add cloudJob.Uri after https://github.com/microsoft/qsharp-runtime/issues/236 is fixed.
                    ("Job ID", cloudJob => cloudJob.Id),
                    ("Job Name", cloudJob => cloudJob.Details.Name),
                    ("Job Status", cloudJob => cloudJob.Status),
                    ("Provider", cloudJob => cloudJob.Details.ProviderId),
                    ("Target", cloudJob => cloudJob.Details.Target),
                    ("Creation Time", cloudJob => cloudJob.Details.CreationTime.ToDateTime()?.ToString()),
                    ("Begin Execution Time", cloudJob => cloudJob.Details.BeginExecutionTime.ToDateTime()?.ToString()),
                    ("End Execution Time", cloudJob => cloudJob.Details.EndExecutionTime.ToDateTime()?.ToString()),
                },
                Rows = jobsList.OrderByDescending(job => job.Details.CreationTime).ToList()
            };
    }

    public class CloudJobToHtmlEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToHtmlDisplayEncoder();

        public string MimeType => MimeTypes.Html;

        public EncodedData? Encode(object displayable) =>
            displayable.AsEnumerableOf<CloudJob>() is IEnumerable<CloudJob> jobs
                ? tableEncoder.Encode(jobs.ToJupyterTable())
                : null;
    }

    public class CloudJobToTextEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToTextDisplayEncoder();

        public string MimeType => MimeTypes.PlainText;

        public EncodedData? Encode(object displayable) =>
            displayable.AsEnumerableOf<CloudJob>() is IEnumerable<CloudJob> jobs
                ? tableEncoder.Encode(jobs.ToJupyterTable())
                : null;
    }

    public class CloudJobJsonConverter : JsonConverter<CloudJob>
    {
        public override CloudJob ReadJson(JsonReader reader, Type objectType, CloudJob existingValue, bool hasExistingValue, JsonSerializer serializer)
            => throw new NotImplementedException();

        public override void WriteJson(JsonWriter writer, CloudJob value, JsonSerializer serializer) =>
            JToken.FromObject(value.ToDictionary()).WriteTo(writer);
    }

    public class CloudJobListJsonConverter : JsonConverter<IEnumerable<CloudJob>>
    {
        public override IEnumerable<CloudJob> ReadJson(JsonReader reader, Type objectType, IEnumerable<CloudJob> existingValue, bool hasExistingValue, JsonSerializer serializer)
            => throw new NotImplementedException();

        public override void WriteJson(JsonWriter writer, IEnumerable<CloudJob> value, JsonSerializer serializer) =>
            JToken.FromObject(value.Select(job => job.ToDictionary())).WriteTo(writer);
    }
}
