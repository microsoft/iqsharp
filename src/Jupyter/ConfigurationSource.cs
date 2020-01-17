// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.Jupyter
{

    public interface IConfigurationSource
    {
        IDictionary<string, JToken> Configuration { get; }

        private T GetOptionOrDefault<T>(string optionName, T defaultValue) =>
            Configuration.TryGetValue(optionName, out var token)
            ? token.ToObject<T>()
            : defaultValue;

        public BasisStateLabelingConvention BasisStateLabelingConvention =>
            GetOptionOrDefault("dump.basisStateLabelingConvention", BasisStateLabelingConvention.LittleEndian);

        public bool TruncateSmallAmplitudes =>
            GetOptionOrDefault("dump.truncateSmallAmplitudes", false);

        public double TruncationThreshold =>
            GetOptionOrDefault("dump.truncationThreshold", 1e-10);
    }

    public class ConfigurationSource : IConfigurationSource
    {
        public IDictionary<string, JToken> Configuration { get; } = new Dictionary<string, JToken>();
    }
}