// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    public enum BasisStateLabelingConvention
    {
        Bitstring,
        LittleEndian,
        BigEndian
    }

    public class DisplayableState
    {
        public IEnumerable<int>? QubitIds { get; set; }
        public int NQubits { get; set; }
        public Complex[]? Amplitudes { get; set; }

        public IEnumerable<(Complex, string)> SignificantAmplitudes(
            IConfigurationSource configurationSource
        ) => SignificantAmplitudes(
            configurationSource.BasisStateLabelingConvention,
            configurationSource.TruncateSmallAmplitudes,
            configurationSource.TruncationThreshold
        );

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
                item => item.Item2
            );

        public string BasisStateLabel(
            BasisStateLabelingConvention convention, int index
        ) => convention switch
            {
                BasisStateLabelingConvention.Bitstring =>
                    System.Convert.ToString(index, 2).PadLeft(NQubits, '0'),
                BasisStateLabelingConvention.LittleEndian =>
                    System.Convert.ToInt64(
                        String.Concat(
                            System.Convert.ToString(index, 2).PadLeft(NQubits, '0').Reverse()
                        ),
                        fromBase: 2
                    )
                    .ToString(),
                BasisStateLabelingConvention.BigEndian =>
                    index.ToString(),
                _ => throw new ArgumentException($"Invalid basis state labeling convention {convention}.")
            };

    }

    public class StateVectorToHtmlResultEncoder : IResultEncoder
    {
        private const double TWO_PI = 2.0 * System.Math.PI;
        public string MimeType => MimeTypes.Html;
        private IConfigurationSource ConfigurationSource;
        public StateVectorToHtmlResultEncoder(IConfigurationSource configurationSource)
        {
            ConfigurationSource = configurationSource;
        }

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

                        return $@"
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
                        ";
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

    public class StateVectorToTextResultEncoder : IResultEncoder
    {
        public string MimeType => MimeTypes.PlainText;
        private IConfigurationSource ConfigurationSource;
        public StateVectorToTextResultEncoder(IConfigurationSource configurationSource)
        {
            ConfigurationSource = configurationSource;
        }

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
