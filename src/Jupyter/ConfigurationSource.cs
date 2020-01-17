// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.Jupyter
{

    public interface IConfigurationSource
    {
        IDictionary<string, JToken> Configuration { get; }

        void Persist();

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
        public IDictionary<string, JToken> Configuration => _Configuration;
        private readonly IDictionary<string, JToken> _Configuration;

        private string ConfigPath =>
            Path.Join(Directory.GetCurrentDirectory(), ".iqsharp-config.json");

        public ConfigurationSource()
        {
            // Try loading configuration from a JSON file in the current working
            // directory.
            if (File.Exists(ConfigPath))
            {
                var configContents = File.ReadAllText(ConfigPath);
                _Configuration = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(
                    configContents,
                    TupleConverters.Converters
                );
            }
            else
            {
                _Configuration = new Dictionary<string, JToken>();
            }
        }

        public void Persist()
        {
            // Try writing the configuration back to JSON.
            File.WriteAllText(
                ConfigPath,
                JsonConvert.SerializeObject(_Configuration, TupleConverters.Converters)
            );
            System.Console.Out.WriteLine($"Wrote config preferences to {ConfigPath}.");
        }
    }
}
