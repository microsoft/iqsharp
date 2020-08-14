// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Tests.qss {
    
    open Microsoft.Quantum.Intrinsic;

    @EntryPoint()
    operation CompareMeasurementResult() : Result {
        using (q = Qubit()) {
            let r = M(q);
            if (r == One) {
                H(q);
                Reset(q);
            }
            return r;
        }
    }
}


