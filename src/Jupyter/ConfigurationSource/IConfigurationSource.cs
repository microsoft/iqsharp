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

        /// <summary>
        ///     Given the name for a configuration and a default value,
        ///     returns the given option if it exists, or the default if the
        ///     user has not specified that option.
        /// </summary>
        /// <param name="optionName">
        ///     Name of the option to be retrieved.
        /// </param>
        /// <param name="defaultValue">
        ///     Value to be returned when the option specified by
        ///     <paramref name="optionName" /> has not been set.
        /// </param>
        /// <typeparam name="T">
        ///     The expected type of the given option.
        /// </typeparam>
        /// <returns>
        ///     The value of the option specified by
        ///     <paramref name="optionName" /> if it exists, otherwise
        ///     the value of <paramref name="defaultValue" />.
        /// </returns>
        public T GetOptionOrDefault<T>(string optionName, T defaultValue) =>
            Configuration.TryGetValue(optionName, out var token)
            ? token.ToObject<T>() ?? defaultValue
            : defaultValue;

        /// <summary>
        ///     The labeling convention to be used when labeling computational
        ///     basis states (bit string, little-endian or big-endian).
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

        /// <summary>
        ///     Allows for options to view phase as arrows, or in radians
        ///     or both in arrow format and radians. This also allows the
        ///     option to show None. 
        /// </summary>
        public PhaseDisplayStyle PhaseDisplayStyle =>
            GetOptionOrDefault("dump.phaseDisplayStyle", PhaseDisplayStyle.ArrowOnly);

        /// <summary>
        ///     Allows for setting the default depth for visualizing Q# operations using the
        ///     <c>%trace</c> command.
        /// </summary>
        public int TraceVisualizationDefaultDepth =>
            GetOptionOrDefault("trace.defaultDepth", 1);

        /// <summary>
        ///     Allows for setting the default visualization style for visualizing Q# operations
        ///     using the <c>%trace</c> command.
        /// </summary>
        public TraceVisualizationStyle TraceVisualizationStyle =>
            GetOptionOrDefault("trace.style", TraceVisualizationStyle.Default);
    }
}
