// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        { }

        public CompilationErrorsException(Diagnostic[] diagnostics) : base("Invalid snippet code")
        {
            this.diagnostics = diagnostics.ToImmutableList();
        }

        public IEnumerable<Diagnostic> ErrorDiagnostics =>
            Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        public IEnumerable<string> Errors =>
            ErrorDiagnostics.Select(QsCompiler.Diagnostics.Formatting.MsBuildFormat);
        private readonly ImmutableList<Diagnostic> diagnostics;
        public IEnumerable<Diagnostic> Diagnostics => diagnostics;
    }
}
