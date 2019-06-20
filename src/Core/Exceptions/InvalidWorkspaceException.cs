// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Quantum.IQSharp.Common
{
    /// <summary>
    /// This Exception gets triggered whenever the user tries to access operations
    /// on the current Workspace, but the Workspace was not correctly built.
    /// </summary>
    public class InvalidWorkspaceException : InvalidOperationException
    {
        public InvalidWorkspaceException(params string[] errors) : base("Invalid workspace")
        {
            this.Errors = errors;
        }

        public string[] Errors { get; }
    }
}
