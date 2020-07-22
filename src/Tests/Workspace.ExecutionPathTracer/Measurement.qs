// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Tests.ExecutionPathTracer {
    
    open Microsoft.Quantum.Measurement;
    open Microsoft.Quantum.Arrays;
    
    operation MResetXCirc() : Unit {
        using (q = Qubit()) {
            let res = MResetX(q);
        }
    }

    operation MResetYCirc() : Unit {
        using (q = Qubit()) {
            let res = MResetY(q);
        }
    }

    operation MResetZCirc() : Unit {
        using (q = Qubit()) {
            let res = MResetZ(q);
        }
    }

    operation ForEachMeasureCirc() : Unit {
        using (qs = Qubit[2]) {
            let res = ForEach(MResetZ, qs);
        }
    }
    
}


