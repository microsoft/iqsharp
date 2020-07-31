// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable

using Microsoft.Jupyter.Core;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    ///     Represents an HTML string to be rendered as an HTML element.
    /// </summary>
    public class DisplayableHtmlElement
    {
        /// <summary>
        ///     Initializes <see cref="DisplayableHtmlElement"/> with the given HTML string.
        /// </summary>
        public DisplayableHtmlElement(string html) => this.Html = html;

        /// <summary>
        ///     HTML string to be rendered.
        /// </summary>
        public string Html { get; }
    }

    /// <summary>
    ///     Encodes <see cref="DisplayableHtmlElement" /> instances as HTML elements.
    /// </summary>
    public class DisplayableHtmlElementEncoder : IResultEncoder
    {
        /// <inheritdoc />
        public string MimeType => MimeTypes.Html;

        /// <summary>
        ///     Checks if a given display object is an <see cref="DisplayableHtmlElement"/>,
        ///     and if so, returns its HTML element.
        /// </summary>
        public EncodedData? Encode(object displayable) =>
            (displayable is DisplayableHtmlElement dis)
                ? dis.Html.ToEncodedData() as EncodedData?
                : null;
    }
}
