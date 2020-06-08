// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    public static class JsonConverters
    {
        private static readonly ImmutableList<JsonConverter> allConverters = ImmutableList.Create<JsonConverter>(
            new CloudJobJsonConverter(),
            new CloudJobListJsonConverter(),
            new TargetStatusJsonConverter(),
            new TargetStatusListJsonConverter()
        );

        public static JsonConverter[] AllConverters => allConverters.ToArray();
    }
}
