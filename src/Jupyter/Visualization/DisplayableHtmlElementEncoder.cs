// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    ///     Represents an HTML string to be rendered as an HTML element.
    /// </summary>
    /// <param name="Html">
    ///     HTML string to be rendered.
    /// </param>
    public record DisplayableHtmlElement(string Html);

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
            (displayable is DisplayableHtmlElement { Html: var html })
                ? html.ToEncodedData() as EncodedData?
                : null;
    }
}
