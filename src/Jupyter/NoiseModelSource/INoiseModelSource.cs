// Copyright (c) Microsoft Corporation
// Licensed under the MIT License.

namespace Microsoft.Quantum.Experimental
{

    public interface INoiseModelSource
    {
        NoiseModel NoiseModel { get; set; }
    }

}
