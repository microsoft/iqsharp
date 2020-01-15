// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable

using System;
using System.Linq;
using System.Numerics;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    public struct StateVector
    {
        public int NQubits { get; set; }
        public Complex[] Amplitudes { get; set; }
    }

    public class StateVectorToHtmlResultEncoder : IResultEncoder
    {
        private const double TWO_PI = 2.0 * System.Math.PI;
        public string MimeType => MimeTypes.Html;

        public EncodedData? Encode(object displayable)
        {
            if (displayable is StateVector vector)
            {
                // First, make a header row.
                var header = @"
                    <thead>
                        <tr>
                            <th>Basis state</th>
                            <th>Amplitude</th>
                            <th>Meas. Pr.</th>
                            <th>Phase</th>
                        </tr>
                    </thead>
                ";

                // Next, make the body by formatting everything as individual rows.
                var formattedData = String.Join("\n",
                    vector.Amplitudes.Select((amplitude, idx) =>
                    {
                        var basisLabel = System.Convert
                            .ToString(idx, 2)
                            .PadLeft(vector.NQubits, '0');

                        return $@"
                            <tr>
                                <td>$\left|{basisLabel}\right\rangle$</td>
                                <td>${amplitude.Real:F4} + {amplitude.Imaginary:F4} i$</td>
                                <td>
                                    <progress max=""100"" value=""{System.Math.Pow(amplitude.Magnitude, 2.0) * 100}""
                                </td>
                                <td style=""transform: rotate({amplitude.Phase * 360.0 / TWO_PI}deg)"">
                                    ↑
                                </td>
                            </tr>
                        ";
                    })
                );

                // Finish by packing everything into the table template.
                var outputTable = $"<table>{header}<tbody>{formattedData}</tbody></table>";
                return outputTable.ToEncodedData();
            }
            else return null;
        }
    }
    
    public class StateVectorToTextResultEncoder : IResultEncoder
    {
        public string MimeType => MimeTypes.PlainText;

        public EncodedData? Encode(object displayable)
        {
            if (displayable is StateVector vector)
            {
                // TODO: refactor to use fancy printing logic from QuantumSimulator.
                //       for now, we do something basic as a placeholder.
                return String.Join("\n",
                    vector.Amplitudes.Select(
                        (amplitude, idx) =>
                        {
                            var basisLabel = System.Convert
                                .ToString(idx, 2)
                                .PadLeft(vector.NQubits, '0');
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
