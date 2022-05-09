// Copyright (c) Microsoft Corporation
// Licensed under the MIT License.

#nullable enable
using Microsoft.Quantum.Experimental;

namespace Microsoft.Quantum.IQSharp.Jupyter;

public record NoiseModelSource : INoiseModelSource
{
    public NoiseModel NoiseModel { get; set; } =
        NoiseModel.TryGetByName("ideal", out var ideal)
        ? ideal
        : throw new Exception("Could not load ideal noise model.");
}
