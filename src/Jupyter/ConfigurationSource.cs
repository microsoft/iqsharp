// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.Jupyter
{

    /// <summary>
    ///     A service that controls configuration options, such as those set
    ///     by preferences, the <c>%config</c> magic command, and so forth.
    /// </summary>
    public interface IConfigurationSource
    {
        /// <summary>
        ///     A dictionary of configuration options available from this
        ///     source.
        /// </summary>
        IDictionary<string, JToken> Configuration { get; }

        /// <summary>
        ///     Persists the current configuration to disk such that a future
        ///     kernel session launched for the same notebook has the given
        ///     configuration.
        /// </summary>
        void Persist();

        private T GetOptionOrDefault<T>(string optionName, T defaultValue) =>
            Configuration.TryGetValue(optionName, out var token)
            ? token.ToObject<T>() ?? defaultValue
            : defaultValue;

        /// <summary>
        ///     The labeling convention to be used when labeling computational
        ///     basis states.
        /// </summary>
        public BasisStateLabelingConvention BasisStateLabelingConvention =>
            GetOptionOrDefault("dump.basisStateLabelingConvention", BasisStateLabelingConvention.LittleEndian);

        /// <summary>
        ///     Whether small amplitudes should be truncated when dumping
        ///     states.
        /// </summary>
        public bool TruncateSmallAmplitudes =>
            GetOptionOrDefault("dump.truncateSmallAmplitudes", false);

        /// <summary>
        ///     The threshold for truncating measurement probabilities when
        ///     dumping states. Computational basis states whose measurement
        ///     probabilities (i.e: squared magnitudes) are below this threshold
        ///     are subject to truncation when
        ///     <see cref="Microsoft.Quantum.IQSharp.Jupyter.IConfigurationSource.TruncateSmallAmplitudes" />
        ///     is <c>true</c>.
        /// </summary>
        public double TruncationThreshold =>
            GetOptionOrDefault("dump.truncationThreshold", 1e-10);
    }

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

        private string ConfigPath =>
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
