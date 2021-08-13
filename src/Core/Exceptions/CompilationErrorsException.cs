// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.IQSharp.Common
{
    public class CompilationErrorsException : InvalidOperationException
    {
        public CompilationErrorsException(QSharpLogger logger) : this(
            logger.Logs.ToArray()
        )
        {
        }

        public CompilationErrorsException(Diagnostic[] diagnostics) : base("Invalid snippet code")
        {
            this.Diagnostics = diagnostics;
        }

        public IEnumerable<Diagnostic> Errors =>
            Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        public IEnumerable<string> FormattedErrors =>
            Errors.Select(QsCompiler.Diagnostics.Formatting.MsBuildFormat);
        public Diagnostic[] Diagnostics { get; }
    }
}
