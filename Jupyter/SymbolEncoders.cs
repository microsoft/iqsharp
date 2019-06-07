// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Collections.Generic;
using Microsoft.Jupyter.Core;
using Markdig;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    ///     Encodes Q# symbols into plain text, e.g. for printing to the console.
    /// </summary>
    public class IQSharpSymbolToTextResultEncoder : IResultEncoder
    {
        public string MimeType => MimeTypes.PlainText;

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
        public string MimeType => MimeTypes.Html;

        public EncodedData? Encode(object displayable)
        {
            if (displayable is IQSharpSymbol symbol)
            {
                var codeLink =
                    $"<a href=\"{symbol.Source}\"><i class=\"fa fas fa-code\"></i></a>";
                return (
                    $"<h4><i class=\"fa fas fa-terminal\"></i> {symbol.Name} {codeLink}</h4>" +
                    Markdown.ToHtml(symbol.Documentation)
                ).ToEncodedData();

            }
            else return null;
        }
    }

}
