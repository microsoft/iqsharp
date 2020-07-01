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
        [JsonProperty("divId")]
        public string DivId { get; set; }
        [JsonProperty("qubit_Ids")]
        public IEnumerable<int>? QubitIds { get; set; }
        [JsonProperty("NQubits_property")]
        public int NQubits { get; set; }
        [JsonProperty("Significant_Amplitudes")]
        public IEnumerable<(Complex, string)> Amplitudes { get; set; }

    }
}

//TODO: Why is are they highlighted in red? 