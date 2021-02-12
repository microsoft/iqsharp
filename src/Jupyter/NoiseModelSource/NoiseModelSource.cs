// Copyright (c) Microsoft Corporation
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.Experimental
{

    public class NoiseModelSource : INoiseModelSource
    {
        public NoiseModel NoiseModel { get; set; } = NoiseModel.Ideal;
    }

}
