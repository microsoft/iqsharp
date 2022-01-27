// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Quantum.IQSharp.Common;
using NuGet.Protocol.Core.Types;

namespace Microsoft.Quantum.IQSharp
{

    internal static class IoExtensions
    {

        public static async IAsyncEnumerable<string> ReadAllLinesAsync(this StreamReader reader)
        {
            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line == null)
                {
                    yield break;
                }
                else
                {
                    yield return line;
                }
            }
        }

    }
}
