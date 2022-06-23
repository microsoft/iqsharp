// Copyright (c) Microsoft Corporation
// Licensed under the MIT License.

#nullable enable
using Microsoft.Quantum.Simulation.OpenSystems.DataModel;

namespace Microsoft.Quantum.IQSharp.Jupyter;

/// <summary>
///     A dependency injection service that stores a noise model for use in
///     open systems simulation.
/// </summary>
public interface INoiseModelSource
{
    /// <summary>
    ///      The current noise model in effect for open systems simulation.
    /// </summary>
    NoiseModel NoiseModel { get; set; }
}
