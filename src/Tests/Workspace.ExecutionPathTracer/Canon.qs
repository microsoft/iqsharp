// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Tests.ExecutionPathTracer {
    
    open Microsoft.Quantum.Canon;
    
    operation ApplyToEachCirc() : Unit {
        using (qs = Qubit[3]) {
            ApplyToEach(H, qs);
            ResetAll(qs);
        }
    }
    
}


