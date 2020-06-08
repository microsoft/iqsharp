// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Jupyter.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal class Histogram : Dictionary<string, double>
    {
    }

    internal static class HistogramExtensions
    {
        internal static Histogram ToHistogram(this MemoryStream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var output = new StreamReader(stream).ReadToEnd();
            var deserializedOutput = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(output);
            var deserializedHistogram = deserializedOutput["Histogram"] as JArray;

            var histogram = new Histogram();
            for (var i = 0; i < deserializedHistogram.Count - 1; i += 2)
            {
                var key = deserializedHistogram[i].ToObject<string>();
                var value = deserializedHistogram[i + 1].ToObject<double>();
                histogram[key] = value;
            }

            return histogram;
        }

        internal static Table<KeyValuePair<string, double>> ToJupyterTable(this Histogram histogram) =>
            new Table<KeyValuePair<string, double>>
            {
                Columns = new List<(string, Func<KeyValuePair<string, double>, string>)>
                    {
                        ("Result", entry => entry.Key),
                        ("Frequency", entry => entry.Value.ToString()),
                    },
                Rows = histogram.ToList()
            };
    }

    public class HistogramToHtmlEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToHtmlDisplayEncoder();

        public string MimeType => MimeTypes.Html;

        public EncodedData? Encode(object displayable)
        {
            return displayable is Histogram histogram
                ? tableEncoder.Encode(histogram.ToJupyterTable())
                : null;
        }
    }

    public class HistogramToTextEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToTextDisplayEncoder();

        public string MimeType => MimeTypes.PlainText;

        public EncodedData? Encode(object displayable)
        {
            return displayable is Histogram histogram
                ? tableEncoder.Encode(histogram.ToJupyterTable())
                : null;
        }
    }
}
