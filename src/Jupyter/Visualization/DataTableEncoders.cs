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
                        (col.ColumnName, row => row.ItemArray[col.Ordinal].ToString())
                    )
                    .ToList(),
                Rows = table.Rows.Cast<DataRow>().ToList()
            };
    }

    public class DataTableToHtmlEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToHtmlDisplayEncoder();
        public string MimeType => MimeTypes.Html;

        public EncodedData? Encode(object displayable) =>
            displayable is DataTable table
            ? tableEncoder.Encode(table.AsJupyterTable())
            : null;
    }

    public class DataTableToTextEncoder : IResultEncoder
    {
        private static readonly IResultEncoder tableEncoder = new TableToTextDisplayEncoder();
        public string MimeType => MimeTypes.PlainText;

        public EncodedData? Encode(object displayable) =>
            displayable is DataTable table
            ? tableEncoder.Encode(table.AsJupyterTable())
            : null;
    }

}
