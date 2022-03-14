// Copyright (c) Microsoft Corporation.
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
    public class TaskProgressToHtmlEncoder : IResultEncoder
    {
        /// <inheritdoc />
        public string MimeType => MimeTypes.Html;

        private EncodedData Wrap(string plainText, int depth) =>
            $"<div style=\"font-size: x-small; text-indent: {1.5 * depth}em\"><tt>{plainText}</tt></div>".ToEncodedData();

        /// <summary>
        ///     Checks if a displayable object represents a list of magic symbol
        ///     summaries, and if so, returns an encoding into an HTML table.
        /// </summary>
        public EncodedData? Encode(object displayable) =>
            displayable is TaskPerformanceArgs perfArgs
            ? Wrap(perfArgs.ToString(), perfArgs.Task.Depth)
            : displayable is TaskCompleteArgs completeArgs
            ? Wrap(completeArgs.ToString(), completeArgs.Task.Depth)
            : (EncodedData?)null;
    }

    // NB: plain text should be handled by just the ToString output.

}
