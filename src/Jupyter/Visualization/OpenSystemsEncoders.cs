// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using NumSharp;
using System.Linq;
using System.Text.Json;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Jupyter;

namespace Microsoft.Quantum.Experimental
{
    // TODO: add display encoders for other formats.
    public class MixedStateToHtmlDisplayEncoder : IResultEncoder
    {
        /// <inheritdoc />
        public string MimeType => MimeTypes.Html;

        public EncodedData? Encode(object displayable)
        {
            if (displayable is MixedState state)
            {
                return $@"
                    <table>
                        <caption>Mixed state</caption>
                        <tr>
                            <th># of qubits</th>
                            <td>{state.NQubits}</td>
                        </tr>

                        <tr>
                            <th>State data</th>
                            <td>
                                $$
                                \left(
                                    \begin{{matrix}}
                                        {state.Data.AsLaTeXMatrixOfComplex()}
                                    \end{{matrix}}
                                \right)
                                $$
                            </td>
                        </tr>
                    </table>
                "
                .ToEncodedData();
            }
            else return null;
        }
    }

    public class NoiseModelToHtmlDisplayEncoder : IResultEncoder
    {
        /// <inheritdoc />
        public string MimeType => MimeTypes.Html;

        public EncodedData? Encode(object displayable)
        {
            if (displayable is NoiseModel noiseModel)
            {
                return $@"
                    <table>
                        <caption>Noise model</caption>
                        <tr>
                            <th>Initial state<th>
                            <td>
                                $$
                                \left( \begin{{matrix}}
                                    {(noiseModel.InitialState as MixedState)?.Data?.AsLaTeXMatrixOfComplex() ?? ""}
                                \end{{matrix}} \right)
                            </td>
                        </tr>
                    </table>
                ".ToEncodedData();
            }
            else return null;
        }
    }
}
