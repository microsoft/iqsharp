// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.Simulation.Simulators;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    internal static class TableExtensions
    {
        internal static Table<MagicSymbolSummary> AsJupyterTable(
            this IEnumerable<MagicSymbolSummary> magicSymbols
        ) =>
            new Table<MagicSymbolSummary>
            {
                Columns = new List<(string, Func<MagicSymbolSummary, string>)>
                {
                    ("Name", symbol => symbol.Name),
                    ("Summary", symbol => symbol.Documentation.Summary),
                    ("Assembly", symbol => symbol.AssemblyName)
                },
                Rows = magicSymbols.ToList()
            };
    }

    /// <summary>
    ///     Encodes results from the <c>%lsmagic</c> magic command as an HTML
    ///     table.
    /// </summary>
    public class MagicSymbolSummariesToHtmlEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToHtmlDisplayEncoder();

        /// <inheritdoc />
        public string MimeType => MimeTypes.Html;

        /// <summary>
        ///     Checks if a displayable object represents a list of magic symbol
        ///     summaries, and if so, returns an encoding into an HTML table.
        /// </summary>
        public EncodedData? Encode(object displayable) =>
            displayable is IEnumerable<MagicSymbolSummary> summaries
            ? tableEncoder.Encode(summaries.AsJupyterTable())
            : null;
    }

    /// <summary>
    ///     Encodes <see cref="System.Data.DataTable" /> instances as plain-text
    ///     tables.
    /// </summary>
    public class MagicSymbolSummariesToTextEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToTextDisplayEncoder();

        /// <inheritdoc />
        public string MimeType => MimeTypes.PlainText;

        /// <summary>
        ///     Checks if a displayable object represents a list of magic symbol
        ///     summaries, and if so, returns an encoding into a plain-text table.
        /// </summary>
        public EncodedData? Encode(object displayable) =>
            displayable is IEnumerable<MagicSymbolSummary> summaries
            ? tableEncoder.Encode(summaries.AsJupyterTable())
            : null;
    }

}
