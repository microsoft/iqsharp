// Copyright (c) Microsoft Corporation
// Licensed under the MIT License.

#nullable enable
using Microsoft.Quantum.Simulation.OpenSystems.DataModel;

namespace Microsoft.Quantum.IQSharp.Jupyter;

/// <summary>
///     A dependency injection service that stores a noise model for use in
///     open systems simulation.
/// </summary>
public record NoiseModelSource : INoiseModelSource
{
    /// <inheritdoc />
    public NoiseModel NoiseModel { get; set; } =
        NoiseModel.TryGetByName("ideal", out var ideal)
        ? ideal
        : throw new Exception("Could not load ideal noise model.");
}
