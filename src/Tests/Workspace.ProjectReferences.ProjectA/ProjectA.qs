// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Tests.ProjectReferences.ProjectA {

    open Microsoft.Quantum.Intrinsic;
    open Tests.ProjectReferences.ProjectB as ProjectB;

    operation RotateAndMeasure(q : Qubit) : Result {
        return ProjectB.RotateAndMeasure(q);
    }
}
