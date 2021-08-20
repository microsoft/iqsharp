// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Quantum.IQSharp.Common;
using NuGet.Protocol.Core.Types;

namespace Microsoft.Quantum.IQSharp
{

    public static class CollectionExtensions
    {
        internal static Dictionary<TKey, TValue> Union<TKey, TValue>(this Dictionary<TKey, TValue> dict, Dictionary<TKey, TValue> other)
        {
            var result = new Dictionary<TKey, TValue>(dict);
            foreach (var item in other)
            {
                result[item.Key] = item.Value;
            }
            return result;
        }
    }
}
