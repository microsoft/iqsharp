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
            logger.Errors.ToArray(),
            logger.Logs.ToArray()
        )
        {
        }

        public CompilationErrorsException(string[] errors, Diagnostic[] diagnostics) : base("Invalid snippet code")
        {
            this.Errors = errors;
            this.Diagnostics = diagnostics;
        }

        public string[] Errors { get; }
        public Diagnostic[] Diagnostics { get; }
    }
}
