// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable

using System.Collections.Generic;
using System.Numerics;
using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    internal class MeasurementHistogramContent : MessageContent
    {
        //[JsonProperty("displayable_state")]
        //public IComparer<string> DisplayableState { get; set; }

        [JsonProperty("div_id")]
        public string DivId { get; set; }

        [JsonProperty("qubit_ids")]
        public IEnumerable<int>? QubitIds { get; set; }

        [JsonProperty("n_qubits")]
        public int NQubits { get; set; }

        [JsonProperty("amplitudes")]
        public IEnumerable<(Complex, string)> Amplitudes { get; set; }

    }
}