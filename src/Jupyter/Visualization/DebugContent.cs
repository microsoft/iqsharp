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
    /// <summary>
    /// Contains information for rendering the status of a client debugging session.
    /// </summary>
    public class DebugStatusContent : MeasurementHistogramContent
    {
        /// <summary>
        /// Contains the identifier of the client debugging session.
        /// </summary>
        [JsonProperty("debug_session")]
        public string DebugSession { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Contains information for rendering the content of a client debugging session.
    /// </summary>
    public class DebugSessionContent : MessageContent
    {   
        /// <summary>
        /// Contains the identifier of the client debugging session.
        /// </summary>
        [JsonProperty("debug_session")]
        public string DebugSession { get; set; } = string.Empty;

        /// <summary>
        /// Contains the identifier of the HTML element for the client to render
        /// the debugging information.
        /// </summary>
        [JsonProperty("div_id")]
        public string DivId { get; set; } = string.Empty;
    }
}
