// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Data;
using System.Linq;
using Microsoft.Jupyter.Core;

namespace Microsoft.Quantum.IQSharp
{
    internal static class TableExtensions
    {
        internal static Table<DataRow> AsJupyterTable(this DataTable table) =>
            new Table<DataRow>
            {
                Columns = table
                    .Columns
                    .Cast<DataColumn>()
                    .Select<DataColumn, (string, Func<DataRow, string>)>(col =>
                        (col.ColumnName, row => row.ItemArray[col.Ordinal]?.ToString() ?? "null")
                    )
                    .ToList(),
                Rows = table.Rows.Cast<DataRow>().ToList()
            };
    }

    /// <summary>
    ///     Encodes <see cref="System.Data.DataTable" /> instances as HTML
    ///     tables.
    /// </summary>
    public class DataTableToHtmlEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToHtmlDisplayEncoder();

        /// <inheritdoc />
        public string MimeType => MimeTypes.Html;

        /// <summary>
        ///     Checks if a displayable object represents a data table, and if
        ///     so, returns an encoding into an HTML table.
        /// </summary>
        public EncodedData? Encode(object displayable) =>
            displayable is DataTable table
            ? tableEncoder.Encode(table.AsJupyterTable())
            : null;
    }

    /// <summary>
    ///     Encodes <see cref="System.Data.DataTable" /> instances as plain-text
    ///     tables.
    /// </summary>
    public class DataTableToTextEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToTextDisplayEncoder();

        /// <inheritdoc />
        public string MimeType => MimeTypes.PlainText;

        /// <summary>
        ///     Checks if a displayable object represents a data table, and if
        ///     so, returns an encoding into a plain-text table.
        /// </summary>
        public EncodedData? Encode(object displayable) =>
            displayable is DataTable table
            ? tableEncoder.Encode(table.AsJupyterTable())
            : null;
    }

}
