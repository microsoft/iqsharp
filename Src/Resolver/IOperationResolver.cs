// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Quantum.IQSharp.Common;

namespace Microsoft.Quantum.IQSharp
{
    public interface IOperationResolver
    {
        OperationInfo Resolve(string input);
    }
}
