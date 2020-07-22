// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Tests.ExecutionPathTracer {
    
    open Microsoft.Quantum.Intrinsic;

    operation HCirc() : Unit {
        using (q = Qubit()) {
            H(q);
            Reset(q);
        }
    }

    operation MCirc() : Unit {
        using (q = Qubit()) {
            let res = M(q);
        }
    }

    operation CnotCirc() : Unit {
        using (qs = Qubit[2]) {
            CNOT(qs[0], qs[1]);
            ResetAll(qs);
        }
    }

    operation CcnotCirc() : Unit {
        using (qs = Qubit[3]) {
            CCNOT(qs[0], qs[2], qs[1]);
            ResetAll(qs);
        }
    }

    operation SwapCirc() : Unit {
        using (qs = Qubit[2]) {
            SWAP(qs[0], qs[1]);
        }
    }

    operation RxCirc() : Unit {
        using (q = Qubit()) {
            Rx(2.0, q);
            Reset(q);
        }
    }

    operation AdjointHCirc() : Unit {
        using (q = Qubit()) {
            Adjoint H(q);
            Reset(q);
        }
    }

    operation ControlledXCirc() : Unit {
        using (qs = Qubit[2]) {
            Controlled X([qs[0]], qs[1]);
            ResetAll(qs);
        }
    }

    operation ControlledAdjointSCirc() : Unit {
        using (qs = Qubit[2]) {
            Controlled Adjoint S([qs[0]], qs[1]);
            ResetAll(qs);
        }
    }
}
