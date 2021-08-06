// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;

namespace Microsoft.Quantum.IQSharp
{
    internal static class NullableExtensions
    {
        internal static T IsNotNull<T>(this T? value)
        {
            Debug.Assert(value != null);
            return value;
        }
    }
}
