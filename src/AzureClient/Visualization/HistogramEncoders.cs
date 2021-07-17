// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
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
        internal static Histogram ToHistogram(this Stream stream, ILogger? logger = null, bool isSimulatorOutput = false)
        {
            var output = new StreamReader(stream).ReadToEnd();
            if (isSimulatorOutput)
            {
                // This routine seems to be using what I think may be an older format
                // (the az quantum cli had handling for both):  {"Histogram":["0",0.5,"1",0.5]}
                output = "{ \"Histogram\" : [ \"" + output.Trim() + "\", 1.0 ] }";

                // TODO: Make this more general. The corresponding implementation from the az quantum cli is:
                //
                //      if job.target.startswith("microsoft.simulator"):
                //      
                //          lines = [line.strip() for line in json_file.readlines()]
                //          result_start_line = len(lines) - 1
                //          if lines[-1].endswith('"'):
                //              while not lines[result_start_line].startswith('"'):
                //                  result_start_line -= 1
                //      
                //          print('\n'.join(lines[:result_start_line]))
                //          result = ' '.join(lines[result_start_line:])[1:-1]  # seems the cleanest version to display
                //          print("_" * len(result) + "\n")
                //      
                //          json_string = "{ \"histogram\" : { \"" + result + "\" : 1 } }"
                //          data = json.loads(json_string)
                //      else:
                //          data = json.load(json_file)
            }

            var deserializedOutput = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(output);

            var histogram = new Histogram();
            if (deserializedOutput["Histogram"] is JArray deserializedHistogram)
            {
                // We expect the histogram to have an even number of entries, organized as:
                // [key, value, key, value, key, value, ...] 
                // If we have an odd number, we will discard the last entry, but we will also log
                // a warning, since this indicates that the data was in an unexpected format.
                if (deserializedHistogram.Count % 2 == 1)
                {
                    logger?.LogWarning($"Expected even number of values in histogram, but found {deserializedHistogram.Count}.");
                }

                for (var i = 0; i < deserializedHistogram.Count - 1; i += 2)
                {
                    var key = deserializedHistogram[i].ToObject<string>();
                    var value = deserializedHistogram[i + 1].ToObject<double>();
                    if (key != null) histogram[key] = value;
                }
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

    /// <summary>
    /// Encodes a <see cref="Histogram"/> object as HTML.
    /// </summary>
    public class HistogramToHtmlEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToHtmlDisplayEncoder();

        /// <inheritdoc/>
        public string MimeType => MimeTypes.Html;

        /// <inheritdoc/>
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


    /// <summary>
    /// Encodes a <see cref="Histogram"/> object as plain text.
    /// </summary>
    public class HistogramToTextEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToTextDisplayEncoder();

        /// <inheritdoc/>
        public string MimeType => MimeTypes.PlainText;

        /// <inheritdoc/>
        public EncodedData? Encode(object displayable) =>
            displayable is Histogram histogram
                ? tableEncoder.Encode(histogram.ToJupyterTable())
                : null;
    }
}
