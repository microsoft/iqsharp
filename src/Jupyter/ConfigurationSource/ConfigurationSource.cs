// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.Jupyter
{

    /// <summary>
    ///     An implementation of the
    ///     <see cref="Microsoft.Quantum.IQSharp.Jupyter.IConfigurationSource" />
    ///     service interface that loads and persists configuration values from
    ///     and to a local JSON file.
    /// </summary>
    public class ConfigurationSource : IConfigurationSource
    {
        /// <inheritdoc />
        public IDictionary<string, JToken> Configuration => _Configuration;
        private readonly IDictionary<string, JToken> _Configuration;

        /// <summary>
        /// The path of the file use to persist configuration into disk.
        /// </summary>
        public static string ConfigPath =>
            Path.Join(Directory.GetCurrentDirectory(), ".iqsharp-config.json");

        /// <summary>
        ///     Constructs a new configuration source, loading initial
        ///     configuration options from the file <c>.iqsharp-config.json</c>,
        ///     if that file exists.
        /// </summary>
        public ConfigurationSource(bool skipLoading = false)
        {
            // Try loading configuration from a JSON file in the current working
            // directory.
            if (!skipLoading && File.Exists(ConfigPath))
            {
                var configContents = File.ReadAllText(ConfigPath);
                var config = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(
                    configContents,
                    JsonConverters.TupleConverters
                );
                Debug.Assert(
                    config != null,
                    "Deserializing JSON configuration resulted in a null value, but no exception was thrown."
                );
                _Configuration = config;
            }
            else
            {
                _Configuration = new Dictionary<string, JToken>();
            }
        }

        /// <summary>
        ///     Persists the current configuration to
        ///     <c>.iqsharp-config.json</c>.
        /// </summary>
        public void Persist()
        {
            // Try writing the configuration back to JSON.
            File.WriteAllText(
                ConfigPath,
                JsonConvert.SerializeObject(_Configuration, JsonConverters.TupleConverters)
            );
            System.Console.Out.WriteLine($"Wrote config preferences to {ConfigPath}.");
        }
    }
}
