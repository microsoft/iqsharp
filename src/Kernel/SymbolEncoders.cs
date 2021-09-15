// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Collections.Generic;
using Microsoft.Jupyter.Core;
using Markdig;
using Microsoft.Quantum.IQSharp.Jupyter;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    ///     Encodes Q# symbols into plain text, e.g. for printing to the console.
    /// </summary>
    public class IQSharpSymbolToTextResultEncoder : IResultEncoder
    {
        /// <inheritdoc />
        public string MimeType => MimeTypes.PlainText;

        /// <summary>
        ///     Checks if a displayable object is an IQ# symbol, and if so,
        ///     returns an encoding of that symbol into plain text.
        /// </summary>
        public EncodedData? Encode(object displayable)
        {
            if (displayable is IQSharpSymbol symbol)
            {
                // TODO: display documentation here.
                //       We will need to parse the documentation to get out the summary, though.
                return $"{symbol.Name}".ToEncodedData();
            }
            else return null;
        }
    }

    /// <summary>
    ///      Encodes Q# symbols into HTML for display in Jupyter Notebooks and
    ///      other similar interfaces.
    /// </summary>
    public class IQSharpSymbolToHtmlResultEncoder : IResultEncoder
    {
        /// <inheritdoc />
        public string MimeType => MimeTypes.Html;

        /// <summary>
        ///     Checks if a displayable object is an IQ# symbol, and if so,
        ///     returns an encoding of that symbol into HTML.
        /// </summary>
        public EncodedData? Encode(object displayable)
        {
            if (displayable is IQSharpSymbol symbol)
            {
                var codeLink =
                    $"<a href=\"{symbol.Source}\"><i class=\"fa fas fa-code\"></i></a>";
                var summary = symbol.Summary != null
                    ? "<h5>Summary</h5>" + Markdown.ToHtml(symbol.Summary)
                    : string.Empty;
                var description = symbol.Description != null
                    ? "<h5>Description</h5>" + Markdown.ToHtml(symbol.Description)
                    : string.Empty;
                return $@"
                    <h4><i class=""fa fas fa-terminal""></i> {symbol.Name} {codeLink}</h4>
                    {summary}
                    {description}
                ".ToEncodedData();

            }
            else return null;
        }
    }

}
