// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Quantum.IQSharp
{
    internal static class LinqExtensions
    {
        internal static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) =>
            // Manually cast here since the compiler can't figure out that
            // the Where call below ensures that elements are non-null.
            (IEnumerable<T>)source.Where(e => e != null);
    }
}