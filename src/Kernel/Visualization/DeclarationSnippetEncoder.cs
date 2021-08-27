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
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.Simulation.Simulators;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.IQSharp.Kernel
{

    public class DeclarationSnippetToHtmlEncoder : IResultEncoder
    {
        private ICompilerService compilerService;
        public DeclarationSnippetToHtmlEncoder(ICompilerService compilerService)
        {
            this.compilerService = compilerService;
        }

        /// <inheritdoc />
        public string MimeType => MimeTypes.Html;

        public EncodedData? Encode(object displayable)
        {
            if (displayable is not DeclarationSnippet snippet)
            {
                return null;
            }

            return $"<pre><code>{compilerService.WrapSnippet(nsName: null, snippet: snippet, openSep: "\n")}</code></pre>".ToEncodedData();
        }
    }

}
