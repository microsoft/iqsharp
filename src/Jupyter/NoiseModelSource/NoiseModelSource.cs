// Copyright (c) Microsoft Corporation
// Licensed under the MIT License.

#nullable enable
using Microsoft.Quantum.Simulation.OpenSystems.DataModel;

namespace Microsoft.Quantum.IQSharp.Jupyter;

/// <inheritdoc />
public record NoiseModelSource : INoiseModelSource
{
    /// <inheritdoc />
    public NoiseModel NoiseModel { get; set; } =
        NoiseModel.TryGetByName("ideal", out var ideal)
        ? ideal
        : throw new Exception("Could not load ideal noise model.");
}
