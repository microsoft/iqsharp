// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    ///     The convention to be used in labeling computational basis states
    ///     given their representations as strings of classical bits.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BasisStateLabelingConvention
    {
        /// <summary>
        ///     Label computational states directly by their bit strings.
        /// </summary>
        /// <example>
        ///     Following this convention, the state |0⟩ ⊗ |1⟩ ⊗ |1⟩ is labeled
        ///     by |011⟩.
        /// </example>
        Bitstring,

        /// <summary>
        ///     Label computational states directly by interpreting their bit
        ///     strings as little-endian encoded integers.
        /// </summary>
        /// <example>
        ///     Following this convention, the state |0⟩ ⊗ |1⟩ ⊗ |1⟩ is labeled
        ///     by |6⟩.
        /// </example>
        LittleEndian,

        /// <summary>
        ///     Label computational states directly by interpreting their bit
        ///     strings as big-endian encoded integers.
        /// </summary>
        /// <example>
        ///     Following this convention, the state |0⟩ ⊗ |1⟩ ⊗ |1⟩ is labeled
        ///     by |3⟩.
        /// </example>
        BigEndian
    }

    /// <summary>
    ///     Represents a quantum state vector and all metadata needed to display
    ///     that state vector.
    /// </summary>
    public class DisplayableState
    {
        private static readonly IComparer<string> ToIntComparer =
            Comparer<string>.Create((label1, label2) =>
                Comparer<int>.Default.Compare(
                    Int32.Parse(label1), Int32.Parse(label2)
                )
            );

        /// <summary>
        ///     The indexes of each qubit on which this state is defined, or
        ///     <c>null</c> if these indexes are not known.
        /// </summary>
        public IEnumerable<int>? QubitIds { get; set; }

        /// <summary>
        ///     The number of qubits on which this state is defined.
        /// </summary>
        public int NQubits { get; set; }

        /// <remarks>
        ///     These amplitudes represent the computational basis states
        ///     labeled in little-endian order, as per the behavior of
        ///     <see cref="Microsoft.Quantum.Simulation.Simulators.QuantumSimulator.StateDumper.Dump" />.
        /// </remarks>
        public Complex[]? Amplitudes { get; set; }

        /// <summary>
        ///     An enumerable source of the significant amplitudes of this state
        ///     vector and their labels, where significance and labels are
        ///     defined by the values loaded from <paramref name="configurationSource" />.
        /// </summary>
        public IEnumerable<(Complex, string)> SignificantAmplitudes(
            IConfigurationSource configurationSource
        ) => SignificantAmplitudes(
            configurationSource.BasisStateLabelingConvention,
            configurationSource.TruncateSmallAmplitudes,
            configurationSource.TruncationThreshold
        );

        /// <summary>
        ///     An enumerable source of the significant amplitudes of this state
        ///     vector and their labels.
        /// </summary>
        /// <param name="convention">
        ///     The convention to be used in labeling each computational basis state.
        /// </param>
        /// <param name="truncateSmallAmplitudes">
        ///     Whether to truncate small amplitudes.
        /// </param>
        /// <param name="truncationThreshold">
        ///     If <paramref name="truncateSmallAmplitudes" /> is <c>true</c>,
        ///     then amplitudes whose absolute value squared are below this
        ///     threshold are suppressed.
        /// </param>
        public IEnumerable<(Complex, string)> SignificantAmplitudes(
            BasisStateLabelingConvention convention,
            bool truncateSmallAmplitudes, double truncationThreshold
        ) =>
            (
                truncateSmallAmplitudes
                ? Amplitudes
                    .Select((amplitude, idx) => (amplitude, idx))
                    .Where(item =>
                        System.Math.Pow(item.amplitude.Magnitude, 2.0) >= truncationThreshold
                    )
                : Amplitudes.Select((amplitude, idx) => (amplitude, idx))
            )
            .Select(
                item => (item.amplitude, BasisStateLabel(convention, item.idx))
            )
            .OrderBy(
                item => item.Item2,
                // If a basis state label is numeric, we want to compare
                // numerically rather than lexographically.
                convention switch {
                    BasisStateLabelingConvention.BigEndian => ToIntComparer,
                    BasisStateLabelingConvention.LittleEndian => ToIntComparer,
                    _ => Comparer<string>.Default
                }
            );

        /// <summary>
        ///     Using the given labeling convention, returns the label for a
        ///     computational basis state described by its bit string as encoded
        ///     into an integer index in the little-endian encoding.
        /// </summary>
        public string BasisStateLabel(
            BasisStateLabelingConvention convention, int index
        ) => convention switch
            {
                BasisStateLabelingConvention.Bitstring =>
                    String.Concat(
                        System
                            .Convert
                            .ToString(index, 2)
                            .PadLeft(NQubits, '0')
                            .Reverse()
                    ),
                BasisStateLabelingConvention.BigEndian =>
                    System.Convert.ToInt64(
                        String.Concat(
                            System.Convert.ToString(index, 2).PadLeft(NQubits, '0').Reverse()
                        ),
                        fromBase: 2
                    )
                    .ToString(),
                BasisStateLabelingConvention.LittleEndian =>
                    index.ToString(),
                _ => throw new ArgumentException($"Invalid basis state labeling convention {convention}.")
            };

    }

    /// <summary>
    ///     A result encoder that displays quantum state vectors as HTML tables.
    /// </summary>
    public class StateVectorToHtmlResultEncoder : IResultEncoder
    {
        private const double TWO_PI = 2.0 * System.Math.PI;

        /// <inheritdoc />
        public string MimeType => MimeTypes.Html;
        private IConfigurationSource ConfigurationSource;

        /// <summary>
        ///     Constructs a new result encoder using configuration settings
        ///     provided by a given configuration source.
        /// </summary>
        public StateVectorToHtmlResultEncoder(IConfigurationSource configurationSource)
        {
            ConfigurationSource = configurationSource;
        }

        /// <summary>
        ///     Checks if a given display object is a state vector, and if so,
        ///     returns its encoding into an HTML table.
        /// </summary>
        public EncodedData? Encode(object displayable)
        {
            string StyleForAngle(double angle) =>
                $@"transform: rotate({angle * 360.0 / TWO_PI}deg);
                   text-align: center;";

            if (displayable is DisplayableState vector)
            {
                // First, print out any qubit IDs if we have them.
                var qubitIdsRow = vector.QubitIds == null
                    ? ""
                    : $@"
                        <tr>
                            <th>Qubit IDs</th>
                            <td span=""3"">{String.Join(", ", vector.QubitIds.Select(id => id.ToString()))}</td>
                        </tr>
                    ";
                // Next, make the body by formatting everything as individual rows.
                var formattedData = String.Join("\n",
                    vector.SignificantAmplitudes(ConfigurationSource).Select(item =>
                    {
                        var (amplitude, basisLabel) = item;

                        return FormattableString.Invariant($@"
                            <tr>
                                <td>$\left|{basisLabel}\right\rangle$</td>
                                <td>${amplitude.Real:F4} {(amplitude.Imaginary >= 0 ? "+" : "")} {amplitude.Imaginary:F4} i$</td>
                                <td>
                                    <progress
                                        max=""100""
                                        value=""{System.Math.Pow(amplitude.Magnitude, 2.0) * 100}""
                                        style=""width: 100%;""
                                    >
                                </td>
                                <td style=""{StyleForAngle(amplitude.Phase)}"">
                                    ↑
                                </td>
                            </tr>
                        ");
                    })
                );

                // Finish by packing everything into the table template.
                var basisWidth = System.Math.Max(6 + vector.NQubits, 20);
                var basisStateMnemonic = ConfigurationSource.BasisStateLabelingConvention switch
                {
                    BasisStateLabelingConvention.Bitstring => " (bitstring)",
                    BasisStateLabelingConvention.LittleEndian => " (little endian)",
                    BasisStateLabelingConvention.BigEndian => " (big endian)",
                    _ => ""
                };
                var outputTable = $@"
                    <table style=""table-layout: fixed; width: 100%"">
                        <thead>
                            {qubitIdsRow}
                            <tr>
                                <th style=""width: {basisWidth}ch)"">Basis state{basisStateMnemonic}</th>
                                <th style=""width: 20ch"">Amplitude</th>
                                <th style=""width: calc(100% - 26ch - {basisWidth}ch)"">Meas. Pr.</th>
                                <th style=""width: 6ch"">Phase</th>
                            </tr>
                        </thead>

                        <tbody>
                            {formattedData}
                        </tbody>
                    </table>
                ";
                return outputTable.ToEncodedData();
            }
            else return null;
        }
    }

    /// <summary>
    ///     A result encoder that displays quantum state vectors as plain-text
    ///     tables.
    /// </summary>
    public class StateVectorToTextResultEncoder : IResultEncoder
    {
        /// <inheritdoc />
        public string MimeType => MimeTypes.PlainText;
        private IConfigurationSource ConfigurationSource;

        /// <summary>
        ///     Constructs a new result encoder using configuration settings
        ///     provided by a given configuration source.
        /// </summary>
        public StateVectorToTextResultEncoder(IConfigurationSource configurationSource)
        {
            ConfigurationSource = configurationSource;
        }

        /// <summary>
        ///     Checks if a given display object is a state vector, and if so,
        ///     returns its encoding into a plain-text table.
        /// </summary>
        public EncodedData? Encode(object displayable)
        {
            if (displayable is DisplayableState vector)
            {
                // TODO: refactor to use fancy printing logic from QuantumSimulator.
                //       for now, we do something basic as a placeholder.
                if (vector.Amplitudes == null)
                {
                    // We can't display a state without any amplitudes!
                    return null;
                }
                return String.Join("\n",
                    vector.SignificantAmplitudes(ConfigurationSource).Select(
                        item =>
                        {
                            var (amplitude, basisLabel) = item;
                            return $"|{basisLabel}⟩\t{amplitude.Real} + {amplitude.Imaginary}𝑖";
                        }
                    )
                )
                .ToEncodedData();
            }
            else return null;
        }
    }
}
