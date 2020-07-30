// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Tests.ProjectReferences {
    
    open Microsoft.Quantum.Intrinsic;
    open Tests.ProjectReferences.ProjectA;

    operation MeasureSingleQubit() : Result {
        using (q = Qubit()) {
            return RotateAndMeasure(q);
        }
    }
}
