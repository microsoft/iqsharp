// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable

using System.Collections.Generic;
using System.Numerics;
using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Microsoft.Quantum.Simulation.Simulators;
using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    /// Contains information for rendering a histogram on the client.
    /// </summary>
    public class MeasurementHistogramContent : MessageContent
    {
        /// <summary>
        /// Information about the state to be displayed.
        /// </summary>
        [JsonProperty("state")]
        public QuantumSimulator.DisplayableState State { get; set; } = new QuantumSimulator.DisplayableState();

        /// <summary>
        /// Unique identifier of the instance.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; } = $"{System.Guid.NewGuid()}";
    }
}
