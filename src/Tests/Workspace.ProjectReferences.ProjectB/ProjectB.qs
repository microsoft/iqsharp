// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Tests.ProjectReferences.ProjectB {
    
    open Microsoft.Quantum.Intrinsic;

    operation RotateAndMeasure(q : Qubit) : Result {
        Rx(1.0, q);
        return M(q);
    }
}
