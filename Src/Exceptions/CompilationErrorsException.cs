// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Quantum.IQSharp.Common
{
    public class CompilationErrorsException : InvalidOperationException
    {
        public CompilationErrorsException(string[] errors) : base("Invalid snippet code")
        {
            this.Errors = errors;
        }

        public string[] Errors { get; }
    }
}
