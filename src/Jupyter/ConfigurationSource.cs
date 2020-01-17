// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    public class ConfigurationSource : IConfigurationSource
    {
        public IDictionary<string, JToken> Configuration { get; } = new Dictionary<string, JToken>();
    }
}