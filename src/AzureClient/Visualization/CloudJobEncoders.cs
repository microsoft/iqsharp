// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Azure.Quantum;
using Microsoft.Jupyter.Core;
using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal static class CloudJobExtensions
    {
        internal static Dictionary<string, object> ToDictionary(this CloudJob cloudJob) =>
            new Dictionary<string, object>()
            {
                { "id", cloudJob.Id },
                { "name", cloudJob.Details.Name },
                { "status", cloudJob.Status },
                { "provider", cloudJob.Details.ProviderId },
                { "target", cloudJob.Details.Target },
            };

        internal static Table<CloudJob> ToJupyterTable(this IEnumerable<CloudJob> jobsList) =>
            new Table<CloudJob>
            {
                Columns = new List<(string, Func<CloudJob, string>)>
                    {
                        ("Job ID", cloudJob => cloudJob.Id),
                        ("Job Name", cloudJob => cloudJob.Details.Name),
                        ("Job Status", cloudJob => cloudJob.Status),
                        ("Provider", cloudJob => cloudJob.Details.ProviderId),
                        ("Target", cloudJob => cloudJob.Details.Target),
                    },
                Rows = jobsList.ToList()
            };
    }

    public class CloudJobToHtmlEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToHtmlDisplayEncoder();

        public string MimeType => MimeTypes.Html;

        public EncodedData? Encode(object displayable)
        {
            if (displayable is CloudJob job) displayable = new List<CloudJob>() { job };

            return displayable is IEnumerable<CloudJob> jobs
                ? tableEncoder.Encode(jobs.ToJupyterTable())
                : null;
        }
    }

    public class CloudJobToTextEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToTextDisplayEncoder();

        public string MimeType => MimeTypes.PlainText;

        public EncodedData? Encode(object displayable)
        {
            if (displayable is CloudJob job) displayable = new List<CloudJob>() { job };

            return displayable is IEnumerable<CloudJob> jobs
                ? tableEncoder.Encode(jobs.ToJupyterTable())
                : null;
        }
    }

    public class CloudJobToJsonEncoder : IResultEncoder
    {
        public string MimeType => MimeTypes.Json;

        public EncodedData? Encode(object displayable)
        {
            if (displayable is CloudJob job) displayable = new List<CloudJob>() { job };

            if (displayable is IEnumerable<CloudJob> jobs)
            {
                var serialized = JsonConvert.SerializeObject(jobs.Select(job => job.ToDictionary()));
                return serialized.ToEncodedData();
            }
            else return null;
        }
    }
}
