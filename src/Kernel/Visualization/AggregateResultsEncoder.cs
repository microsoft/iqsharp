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
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    ///     Encodes results from the <c>%lsmagic</c> magic command as an HTML
    ///     table.
    /// </summary>
    public class AggregateResultsToHtmlEncoder : IResultEncoder
    {
        private readonly IQSharpEngine engine;
        public AggregateResultsToHtmlEncoder(IExecutionEngine engine)
        {
            this.engine = (IQSharpEngine)engine;
        }

        /// <inheritdoc />
        public string MimeType => MimeTypes.Html;

        /// <summary>
        ///     Checks if a displayable object represents a list of magic symbol
        ///     summaries, and if so, returns an encoding into an HTML table.
        /// </summary>
        public EncodedData? Encode(object displayable)
        {
            if (displayable is not IQSharpEngine.AggregateResults results)
            {
                return null;
            }

            return string.Join(
                "<hr>\n",
                results.Results.Select(result =>
                    // FIXME: This depends on new methods in jupyter-core
                    //        not yet released to nuget.org.
                    engine.TryEncodeAs(result.Output, MimeType, out var encoded) && encoded is { Data: var data }
                    ? data
                    : result.Output is {} output
                    ? $"<pre>{output}</pre>"
                    : null
                )
                .Where(result => result != null)
            ).ToEncodedData();
        }
    }

}
