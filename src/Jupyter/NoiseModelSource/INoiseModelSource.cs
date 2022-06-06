// Copyright (c) Microsoft Corporation
// Licensed under the MIT License.

#nullable enable
using Microsoft.Quantum.Simulation.OpenSystems.DataModel;

namespace Microsoft.Quantum.IQSharp.Jupyter;

public interface INoiseModelSource
{
    NoiseModel NoiseModel { get; set; }
}
