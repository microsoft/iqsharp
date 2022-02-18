// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Azure.Quantum;
using Microsoft.Identity.Client;
using Microsoft.Jupyter.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    /// Encodes a <see cref="DeviceCodeResult"/> object as HTML.
    /// </summary>
    public class CostEstimateToHtmlEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToHtmlDisplayEncoder();

        /// <inheritdoc/>
        public string MimeType => MimeTypes.Html;

        /// <inheritdoc/>
        public EncodedData? Encode(object displayable)
        {
            if (displayable is SimulatedCostEstimate costEstimate)
            {
                // This is a little bit of a hack to get rows as major instead
                // of columns.
                var outerTable = new Table<(string, string, string)>
                {
                    Columns = new List<(string, Func<(string, string, string), string>)>
                    {
                        ("Name", col => col.Item1),
                        ("Value", col => col.Item2),
                        ("Unit", col => col.Item3)
                    },
                    Rows = new List<(string, string, string)>
                    {
                        ("Estimated Total", $"{costEstimate.EstimatedTotal:F2}", costEstimate.CurrencyCode),
                    }
                    .Concat(
                        costEstimate.Events.Select(ev =>
                            (ev.DimensionName, $"{ev.AmountConsumed}", ev.MeasureUnit)
                        )
                    )
                    .ToList()
                };

                return tableEncoder.Encode(outerTable);
            } else return null;
        }
    }

    // /// <summary>
    // /// Encodes a <see cref="DeviceCodeResult"/> object as plain text.
    // /// </summary>
    // public class DeviceCodeResultToTextEncoder : IResultEncoder
    // {
    //     private static readonly IResultEncoder tableEncoder = new TableToHtmlDisplayEncoder();

    //     /// <inheritdoc/>
    //     public string MimeType => MimeTypes.PlainText;

    //     /// <inheritdoc/>
    //     public EncodedData? Encode(object displayable) =>
    //         displayable is DeviceCodeResult deviceCode
    //             ? deviceCode.Message.ToEncodedData()
    //             : null as EncodedData?;
    // }
}
