// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using NumSharp;
using System.Linq;
using System.Text.Json;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Jupyter;
using System.Collections.Generic;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;

namespace Microsoft.Quantum.Experimental
{
    /// <summary>
    ///     Represents different styles for displaying the Q# execution path
    ///     visualization as HTML.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum StabilizerStateVisualizationStyle
    {
        MatrixWithDestabilizers,
        MatrixWithoutDestabilizers,
        DenseGroupPresentation,
        SparseGroupPresentation
    }

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

    public class StabilizerStateToHtmlDisplayEncoder : IResultEncoder
    {
        private readonly IConfigurationSource config;

        /// <inheritdoc />
        public string MimeType => MimeTypes.Html;

        public StabilizerStateToHtmlDisplayEncoder(IConfigurationSource config)
        {
            this.config = config;
        }

        /// <inheritdoc />
        public EncodedData? Encode(object displayable)
        {
            if (displayable is StabilizerState { NQubits: var nQubits, Data: {} data })
            {
                var repeatedCs = new String('c', nQubits);
                var colspec = $"{repeatedCs}|{repeatedCs}|c";
                return $@"
                    <table>
                        <caption>Stabilizer state</caption>
                        <tr>
                            <th># of qubits</th>
                            <td>{nQubits}</td>
                        </tr>

                        <tr>
                            <th>State data</th>
                            <td>{
                                config.ExperimentalSimulatorStabilizerStateVisualizationStyle switch
                                {
                                    StabilizerStateVisualizationStyle.MatrixWithDestabilizers =>
                                        $@"$$\left(\begin{{array}}{{{colspec}}}{
                                            string.Join(
                                                "\\\\\n",
                                                Enumerable
                                                    .Range(0, data.Shape[0])
                                                    .Select(
                                                        idxRow => 
                                                            (
                                                                idxRow == nQubits
                                                                ? "\\hline\n"
                                                                : ""
                                                            ) + string.Join(" & ",
                                                                Enumerable.Range(0, data.Shape[1])
                                                                .Select(
                                                                    idxCol => data[idxRow, idxCol] ? "1" : "0"
                                                                )
                                                        )
                                                    )
                                            )
                                        }\end{{array}}\right)$$",
                                    StabilizerStateVisualizationStyle.MatrixWithoutDestabilizers =>
                                        $@"$$\left(\begin{{array}}{{{colspec}}}{
                                            string.Join(
                                                "\\\\\n",
                                                Enumerable
                                                    .Range(nQubits, data.Shape[0] / 2)
                                                    .Select(
                                                        idxRow => string.Join(" & ",
                                                            Enumerable.Range(0, data.Shape[1])
                                                            .Select(
                                                                idxCol => data[idxRow, idxCol] ? "1" : "0"
                                                            )
                                                        )
                                                    )
                                            )
                                        }\end{{array}}\right)$$",
                                    StabilizerStateVisualizationStyle.DenseGroupPresentation =>
                                        $@"$$\left\langle {
                                            string.Join(
                                                ", ",
                                                Enumerable
                                                    .Range(nQubits, data.Shape[0] / 2)
                                                    .Select(
                                                        idxRow => string.Join("",
                                                            Enumerable.Range(0, nQubits)
                                                                .Select(idxQubit =>
                                                                {
                                                                    (bool x, bool z) = (data[idxRow, idxQubit], data[idxRow, nQubits + idxQubit]);
                                                                    return (x, z) switch
                                                                    {
                                                                        (false, false) => "𝟙",
                                                                        (true, false) => "X",
                                                                        (false, true) => "Z",
                                                                        (true, true) => "Y"
                                                                    };
                                                                })
                                                            )
                                                    )
                                            )
                                        } \right\rangle$$",
                                        // "(dense group presentation not yet implemented)",
                                    StabilizerStateVisualizationStyle.SparseGroupPresentation =>
                                        "(sparse group presentation not yet implemented)",
                                    var unknown => throw new Exception($"Invalid visualization style {unknown}.")
                                }
                            }</td>
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
                                        MixedPauliProcess { Operators: {} ops } => 
                                            $@"
                                                \text{{(mixed Pauli process) }}
                                                \left\{{{
                                                    string.Join(", ",
                                                        ops.Select(
                                                            item => $@"{item.Item1} {string.Join(
                                                                "",
                                                                item.Item2.Select(pauli => pauli.ToString())
                                                            )}"
                                                        )
                                                    )
                                                }\right\}}
                                            ",
                                        {} unknown => unknown.ToString(),
                                        null => "<null>"
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
                            <th>$Z$-measurement</th>
                            <td>{
                                noiseModel?.ZMeas switch
                                {
                                    EffectsInstrument { Effects: var effects } => $@"
                                        $$\left\{{{
                                            string.Join(", ",
                                                (effects ?? new List<Process>())
                                                    .Select(
                                                        process => $@"
                                                            \left( \begin{{matrix}}
                                                                {(process as ArrayProcess)?.Data?.AsLaTeXMatrixOfComplex() ?? ""}
                                                            \end{{matrix}} \right)
                                                        "
                                                    )
                                            )
                                        }\right\}}$$
                                    ",
                                    ZMeasurementInstrument { PrReadoutError: var prReadoutError } =>
                                        $@"$Z$-basis measurement w/ error probability {prReadoutError}",
                                    {} unknown => unknown.ToString(),
                                    null => "<null>"
                                }
                            }</td>
                        </tr>
                    </table>
                ".ToEncodedData();
            }
            else return null;
        }
    }
}
