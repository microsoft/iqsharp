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
    public class DebugStatusContent : MeasurementHistogramContent
    {
        [JsonProperty("debug_session")]
        public string DebugSession { get; set; }
    }
    
    public class DebugSessionContent : MessageContent
    {   
        [JsonProperty("debug_session")]
        public string DebugSession { get; set; }

        [JsonProperty("div_id")]
        public string DivId { get; set; }
    }
}
