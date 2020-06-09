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
            if (displayable is Histogram histogram)
            {
                var style = "text-align: left";
                var columnStyle = $"{style}; width: 25ch";
                var lastColumnStyle = $"{style}; width: calc(100% - 25ch - 25ch)";

                // Make the HTML table body by formatting everything as individual rows.
                var formattedData = string.Join("\n",
                    histogram.Select(entry =>
                    {
                        var result = entry.Key;
                        var frequency = entry.Value;

                        return FormattableString.Invariant($@"
                            <tr>
                                <td style=""{columnStyle}"">{result}</td>
                                <td style=""{columnStyle}"">{frequency}</td>
                                <td style=""{lastColumnStyle}"">
                                    <progress
                                        max=""100""
                                        value=""{frequency * 100}""
                                        style=""width: 100%;""
                                    >
                                </td>
                            </tr>
                        ");
                    })
                );

                // Construct and return the table.
                var outputTable = $@"
                    <table style=""table-layout: fixed; width: 100%"">
                        <thead>
                            <tr>
                                <th style=""{columnStyle}"">Result</th>
                                <th style=""{columnStyle}"">Frequency</th>
                                <th style=""{lastColumnStyle}"">Histogram</th>
                            </tr>
                        </thead>
                        <tbody>
                            {formattedData}
                        </tbody>
                    </table>
                ";
                return outputTable.ToEncodedData();
            }
            else return null;
        }
    }

    public class HistogramToTextEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToTextDisplayEncoder();

        public string MimeType => MimeTypes.PlainText;

        public EncodedData? Encode(object displayable) =>
            displayable is Histogram histogram
                ? tableEncoder.Encode(histogram.ToJupyterTable())
                : null;
    }
}
