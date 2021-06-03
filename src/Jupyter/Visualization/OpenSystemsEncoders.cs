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
            string RowForProcess(string name, Process? process) =>
                $@"
                        <tr>
                            <th>{name}</th>
                            <td>
                                $${
                                    // NB: We use a switch expression here to
                                    //     pattern match on what representation
                                    //     the given process is expressed in.
                                    //     In doing so, we use the {} pattern
                                    //     to capture non-null process data,
                                    //     so that we are guaranteed that we
                                    //     only try to display processs that
                                    //     were successfully deserialized.
                                    // TODO: Add other variants of process here.
                                    process switch
                                    {
                                        UnitaryProcess { Data: {} data } =>
                                            $@"
                                                \left( \begin{{matrix}}
                                                    {data.AsLaTeXMatrixOfComplex() ?? ""}
                                                \end{{matrix}} \right)",
                                        KrausDecompositionProcess { Data: {} data } =>
                                            $@"
                                                \left\{{{
                                                    string.Join(", ",
                                                        Enumerable
                                                            .Range(0, data.Shape[0])
                                                            .Select(idxKrausOperator =>
                                                                $@"
                                                                    \left( \begin{{matrix}}
                                                                        {data[idxKrausOperator].AsLaTeXMatrixOfComplex()}
                                                                    \end{{matrix}} \right)
                                                                "
                                                            )
                                                    )
                                                }\right\}}
                                            ",
                                        _ => ""
                                    }
                                }$$
                            </td>
                        </tr>
                ";

            if (displayable is NoiseModel noiseModel)
            {
                return $@"
                    <table>
                        <caption>Noise model</caption>
                        <tr>
                            <th>Initial state</th>
                            <td>
                                $$
                                    \left( \begin{{matrix}}
                                        {(noiseModel.InitialState as MixedState)?.Data?.AsLaTeXMatrixOfComplex() ?? ""}
                                    \end{{matrix}} \right)
                                $$
                            </td>
                        </tr>
                        {RowForProcess("CNOT", noiseModel?.Cnot)}
                        {RowForProcess("$I$", noiseModel?.I)}
                        {RowForProcess("$X$", noiseModel?.X)}
                        {RowForProcess("$Y$", noiseModel?.Y)}
                        {RowForProcess("$Z$", noiseModel?.Z)}
                        {RowForProcess("$H$", noiseModel?.H)}
                        {RowForProcess("$S$", noiseModel?.S)}
                        {RowForProcess("$S^{\\dagger}$", noiseModel?.SAdj)}
                        {RowForProcess("$T$", noiseModel?.T)}
                        {RowForProcess("$T^{\\dagger}$", noiseModel?.TAdj)}
                        <tr>
                            <th>$Z$-measurement effects</th>
                            <td>
                                $$
                                    \left\{{
                                        {
                                            string.Join(", ",
                                                // TODO: visualize other kinds of effects here.
                                                ((noiseModel?.ZMeas as EffectsInstrument)?.Effects ?? new List<Process>())
                                                    .Select(
                                                        process => $@"
                                                            \left( \begin{{matrix}}
                                                                {process?.Data?.AsLaTeXMatrixOfComplex() ?? ""}
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
