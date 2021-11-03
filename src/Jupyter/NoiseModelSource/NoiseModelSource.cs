// Copyright (c) Microsoft Corporation
// Licensed under the MIT License.

namespace Microsoft.Quantum.Experimental
{

    public class NoiseModelSource : INoiseModelSource
    {
        public NoiseModel NoiseModel { get; set; } =
            NoiseModel.TryGetByName("ideal", out var ideal)
            ? ideal
            : throw new Exception("Could not load ideal noise model.");
    }

}
