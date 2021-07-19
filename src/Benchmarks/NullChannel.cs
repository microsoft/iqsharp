// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Jupyter.Core;

namespace Microsoft.Quantum.IQSharp.Benchmarks
{
    
    internal class NullChannel : IChannel
    {
        public void Display(object displayable)
        {
        }

        public void Stderr(string message)
        {
        }

        public void Stdout(string message)
        {
        }
    }
}
