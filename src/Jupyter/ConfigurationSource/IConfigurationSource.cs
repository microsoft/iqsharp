// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Quantum.Experimental;
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
        ///     Allows for options to view measurement as horizontal histograms for each
        ///     basis state, as well as the option to toggle measurement probability as
        ///     a decimal, progress bar, both, or None. 
        /// </summary>
        public MeasurementDisplayStyle MeasurementDisplayStyle =>
            GetOptionOrDefault("dump.measurementDisplayStyle", MeasurementDisplayStyle.BarAndNumber);

        /// <summary>
        ///     Allows for options to change measurement probability precision when viewing
        ///     measurement <span class="x x-first x-last">probabilities</span> in a decimal format. 
        /// </summary>
        public int MeasurementDisplayPrecision =>
            GetOptionOrDefault("dump.measurementDisplayPrecision", 4);

        /// <summary>
        ///     Allows for option to view basis states as a function of
        ///     measurement probability in a vertical histogram format. 
        /// </summary>
        public bool MeasurementDisplayHistogram =>
            GetOptionOrDefault("dump.measurementDisplayHistogram", false);

        /// <summary>
        ///     Whether to use only a simple plain-text encoding when dumping states.
        /// </summary>
        public bool PlainTextOnly =>
            GetOptionOrDefault("dump.plainTextOnly", false);

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

        /// <summary>
        ///     Specifies the number of qubits that the experimental simulators
        ///     support for use in running Q# programs.
        /// </summary>
        public uint ExperimentalSimulatorCapacity =>
            GetOptionOrDefault<uint>("experimental.simulators.nQubits", 3);

        /// <summary>
        ///     Specifies the representation to use for the initial state
        ///     when simulating Q# programs with experimental simulators.
        /// </summary>
        public string ExperimentalSimulatorRepresentation =>
            GetOptionOrDefault<string>("experimental.simulators.representation", "mixed");

        /// <summary>
        ///     Specifies the format used in dumping stabilizer states.
        /// <summary>
        public StabilizerStateVisualizationStyle ExperimentalSimulatorStabilizerStateVisualizationStyle =>
            GetOptionOrDefault<StabilizerStateVisualizationStyle>("experimental.simulators.stabilizerStateStyle", StabilizerStateVisualizationStyle.MatrixWithDestabilizers);
    }
}
