// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.IQSharp
{
    public interface IOperationResolver
    {
        OperationInfo? Resolve(string input);
    }
}
