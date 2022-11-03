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
    public class DisplayableStateWithId : CommonNativeSimulator.DisplayableState
    {
        public string? Id { get; set; }
    }

    /// <summary>
    ///     Represents different styles for displaying the phases of complex
    ///     amplitudes when displaying state vectors as HTML.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PhaseDisplayStyle
    {
        /// <summary>
        ///     Suppress phase information.
        /// </summary>
        None,
        /// <summary>
        ///     Display phase information as an arrow (<c>↑</c>) rotated by an angle
        ///     dependent on the phase.
        /// </summary>
        ArrowOnly,
        /// <summary> 
        ///     Display phase information in number format.
        /// </summary>
        NumberOnly,
        /// <summary>
        ///     Display phase information as an arrow (<c>↑</c>) rotated by an angle
        ///     dependent on the phase as well as display phase information in number
        ///     format.
        /// </summary>
        ArrowAndNumber
    }

    /// <summary> 
    ///     Represents different styles for displaying the measurement probability
    ///     of state vectors as HTML.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MeasurementDisplayStyle
    {
        /// <summary>
        ///     Suppress measurement probability information.
        /// </summary>
        None,
        /// <summary>
        ///     Display measurement probability information as a horizontal histogram.
        /// </summary>
        BarOnly,
        /// <summary>
        ///     Display measurement probability information as a numerical percentage.
        /// </summary>
        NumberOnly,
        /// <summary>
        ///     Display measurement probability information as a horizontal histogram as well
        ///     in a numerical percentage format.
        /// </summary>
        BarAndNumber
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
            if (ConfigurationSource.PlainTextOnly) return null;

            string StyleForAngle(double angle) =>
                $@"transform: rotate({angle * 360.0 / TWO_PI}deg);
                   text-align: center;";
            string StyleForNumber() =>
                $@"text-align: center;";

            if (displayable is CommonNativeSimulator.DisplayableState vector)
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

                        // Different options for displaying phase style.
                        var phaseCell = ConfigurationSource.PhaseDisplayStyle switch
                        {
                            PhaseDisplayStyle.None => "",
                            PhaseDisplayStyle.ArrowOnly => FormattableString.Invariant($@"
                                <td style=""{StyleForAngle(amplitude.Phase)}"">
                                 ↑
                                </td>
                            "),
                            PhaseDisplayStyle.ArrowAndNumber => FormattableString.Invariant($@"
                                <td>
                                 <div style=""{StyleForAngle(amplitude.Phase)}""> ↑ </div>
                                 <div style=""{StyleForNumber()}"">{amplitude.Phase:F4}</div>
                                </td>
                            "),
                            PhaseDisplayStyle.NumberOnly => FormattableString.Invariant($@"
                                <td> 
                                 {amplitude.Phase:F4}
                                </td>
                            "),
                            _ => throw new ArgumentException($"Unsupported style {ConfigurationSource.PhaseDisplayStyle}")
                        };
                        
                        // Different options for displaying measurement style.
                        var measurementHistogram = ConfigurationSource.MeasurementDisplayHistogram;
                        var measurementPrecision = ConfigurationSource.MeasurementDisplayPrecision;
                        
                        var elementId = $"round-{System.Guid.NewGuid()}"; // Randomly generate an ID for each <p> element.
                        var measurementCell = ConfigurationSource.MeasurementDisplayStyle switch
                        {
                            MeasurementDisplayStyle.None => String.Empty,
                            MeasurementDisplayStyle.BarOnly => FormattableString.Invariant($@"
                                <td>
                                    <progress
                                        max=""100""
                                        value=""{System.Math.Pow(amplitude.Magnitude, 2.0) * 100}""
                                        style=""width: 100%;""
                                    >
                                </td>
                            "),
                            MeasurementDisplayStyle.BarAndNumber => FormattableString.Invariant($@"
                                <td>
                                    <progress
                                        max=""100""
                                        value=""{System.Math.Pow(amplitude.Magnitude, 2.0) * 100}""
                                        style=""width: 100%;""
                                    > 
                                    <td>
                                    <p id=""{elementId}""> 
                                    <script>
                                    var num = {System.Math.Pow(amplitude.Magnitude, 2.0) * 100};
                                    num = num.toFixed({measurementPrecision});
                                    var num_string = num + ""%"";
                                     document.getElementById(""{elementId}"").innerHTML = num_string;
                                    </script> </p>
                                    </td>
                                </td>
                            "), 
                            
                            MeasurementDisplayStyle.NumberOnly => FormattableString.Invariant($@"
                                <td> 
                                    <p id=""{elementId}"" style=""text-align: right""> 
                                    <script>
                                    var num = {System.Math.Pow(amplitude.Magnitude, 2.0) * 100};
                                    num = num.toFixed({measurementPrecision});
                                    var num_string = num + ""%"";
                                     document.getElementById(""{elementId}"").innerHTML = num_string;
                                    </script> </p>
                                    
                                </td>
                            "),
                            _ => throw new ArgumentException($"Unsupported style {ConfigurationSource.MeasurementDisplayStyle}")
                        };

                        // Construct and return the full table row for this basis state.
                        return FormattableString.Invariant($@"
                            <tr>
                                <td>$\left|{basisLabel}\right\rangle$</td>
                                <td>${amplitude.Real:F4} {(amplitude.Imaginary >= 0 ? "+" : "")} {amplitude.Imaginary:F4} i$</td>
                                {measurementCell}
                                {phaseCell}
                            </tr>
                        ");
                    })
                );

                // Finish by packing everything into the table template.
                var basisWidth = System.Math.Max(6 + vector.NQubits, 20);
                var basisStateMnemonic = ConfigurationSource.BasisStateLabelingConvention switch
                {
                    CommonNativeSimulator.BasisStateLabelingConvention.Bitstring => " (bitstring)",
                    CommonNativeSimulator.BasisStateLabelingConvention.LittleEndian => " (little endian)",
                    CommonNativeSimulator.BasisStateLabelingConvention.BigEndian => " (big endian)",
                    _ => ""
                };
                
                var outputTable = $@"
                    <table style=""table-layout: fixed; width: 100%"">
                        <thead>
                            {qubitIdsRow}
                            <tr>
                                <th style=""width: {basisWidth}ch)"">Basis state{basisStateMnemonic}</th>
                                <th style=""width: 20ch"">Amplitude</th>";
                if (ConfigurationSource.MeasurementDisplayStyle != MeasurementDisplayStyle.None) {
                    outputTable += $@"<th style=""width: calc(100% - 26ch - {basisWidth}ch)"">Meas. Pr.</th>";

                };
                if (ConfigurationSource.PhaseDisplayStyle != PhaseDisplayStyle.None) {
                    outputTable += $@"<th style=""width: 6ch"">Phase</th>";

                };
                outputTable += $@"
                            </tr>
                        </thead>
                        <tbody>
                        {formattedData}
                        </tbody>
                    </table>";
                
                if (ConfigurationSource.MeasurementDisplayHistogram && vector is DisplayableStateWithId { Id: var id })
                {
                    outputTable += $@"<div id=""{id}""></div>";
                };
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
            if (displayable is CommonNativeSimulator.DisplayableState vector)
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
