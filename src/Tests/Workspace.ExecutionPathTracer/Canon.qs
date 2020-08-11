// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Tests.ExecutionPathTracer {
    
    open Microsoft.Quantum.Intrinsic;
    open Mock.Standard;

    operation ApplyToEachCirc() : Unit {
        using (qs = Qubit[3]) {
            ApplyToEach(H, qs);
            ResetAll(qs);
        }
    }

    operation ApplyDoubleX(q : Qubit) : Unit {
        X(q);
        X(q);
    }

    operation ApplyToEachDepth2Circ() : Unit {
        using (qs = Qubit[2]) {
            ApplyToEach(ApplyDoubleX, qs);
            ResetAll(qs);
        }
    }

}


