﻿// Copyright (c) Microsoft Corporation.
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
    /// <summary>
    /// Declares classes derived from <see cref="JsonConverter"/> defined in this assembly.
    /// </summary>
    public static class JsonConverters
    {
        private static readonly ImmutableList<JsonConverter> allConverters = ImmutableList.Create<JsonConverter>(
            new CloudJobJsonConverter(),
            new CloudJobListJsonConverter(),
            new TargetStatusJsonConverter(),
            new TargetStatusListJsonConverter(),
            new AzureClientErrorJsonConverter()
        );

        /// <summary>
        /// Gets an array of instances of each class derived from <see cref="JsonConverter"/> defined in this assembly.
        /// </summary>
        public static JsonConverter[] AllConverters => allConverters.ToArray();
    }
}
