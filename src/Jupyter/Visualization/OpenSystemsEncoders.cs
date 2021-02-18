// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using NumSharp;
using System.Linq;
using System.Text.Json;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Jupyter;
using System.Collections.Generic;

namespace Microsoft.Quantum.Experimental
{
    // TODO: add display encoders for other formats.
    public class MixedStateToHtmlDisplayEncoder : IResultEncoder
    {
        /// <inheritdoc />
        public string MimeType => MimeTypes.Html;

        /// <inheritdoc />
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

        /// <inheritdoc />
        public EncodedData? Encode(object displayable)
        {
            string RowForChannel(string name, Channel? channel) =>
                $@"
                        <tr>
                            <th>{name}</th>
                            <td>
                                $$
                                    \left( \begin{{matrix}}
                                        {channel?.Data?.AsLaTeXMatrixOfComplex() ?? ""}
                                    \end{{matrix}} \right)
                                $$
                            </td>
                        </tr>
                ";

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
                                $$
                            </td>
                        </tr>
                        {RowForChannel("CNOT", noiseModel?.Cnot)}
                        {RowForChannel("$I$", noiseModel?.I)}
                        {RowForChannel("$X$", noiseModel?.X)}
                        {RowForChannel("$Y$", noiseModel?.Y)}
                        {RowForChannel("$Z$", noiseModel?.Z)}
                        {RowForChannel("$H$", noiseModel?.H)}
                        {RowForChannel("$S$", noiseModel?.S)}
                        {RowForChannel("$S^{\\dagger}$", noiseModel?.SAdj)}
                        {RowForChannel("$T$", noiseModel?.T)}
                        {RowForChannel("$T^{\\dagger}$", noiseModel?.TAdj)}
                        <tr>
                            <th>$Z$-measurement effects</th>
                            <td>
                                $$
                                    \left\{{
                                        {
                                            string.Join(", ",
                                                (noiseModel?.ZMeas?.Effects ?? new List<Channel>())
                                                    .Select(
                                                        channel => $@"
                                                            \left( \begin{{matrix}}
                                                                {channel?.Data?.AsLaTeXMatrixOfComplex() ?? ""}
                                                            \end{{matrix}} \right)
                                                        "
                                                    )
                                            )
                                        }
                                    \right\}}
                                $$
                            </td>
                        </tr>
                    </table>
                ".ToEncodedData();
            }
            else return null;
        }
    }
}
