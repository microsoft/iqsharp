using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Core.ExecutionPathTracer;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    ///     Contains the ID of the div used to populate the
    ///     <see cref="ExecutionPath"/> visualization.
    /// </summary>
    public class ExecutionPathDisplayable
    {
        /// <summary>
        ///     Initializes <see cref="ExecutionPathDisplayable"/> with the given <c>id</c>.
        /// </summary>
        public ExecutionPathDisplayable(string id) =>
            this.Id = id;

        /// <summary>
        ///     ID of the HTML div that will contain the visualization.
        /// </summary>
        public string Id { get; }
    }

    /// <summary>
    ///     Encodes <see cref="ExecutionPathDisplayable" /> instances as HTML divs.
    /// </summary>
    public class ExecutionPathToHtmlEncoder : IResultEncoder
    {
        /// <inheritdoc />
        public string MimeType => MimeTypes.Html;

        /// <summary>
        ///     Checks if a given display object is an <see cref="ExecutionPathDisplayable"/>,
        ///     and if so, returns the HTML div with the corresponding id that will contain the
        ///     <see cref="ExecutionPath"/> visualization.
        /// </summary>
        public EncodedData? Encode(object displayable) =>
            (displayable is ExecutionPathDisplayable dis)
                ? $"<div id='{dis.Id}' />".ToEncodedData() as EncodedData?
                : null;
    }
}
