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

    // Custom operation
    operation Foo(theta : Double, (qubit : Qubit, bar : String)) : Unit
    is Adj + Ctl {
    }

    operation FooCirc() : Unit {
        using (q = Qubit()) {
            Foo(2.1, (q, "bar"));
        }
    }

    operation ControlledFooCirc() : Unit {
        using (qs = Qubit[2]) {
            Controlled Foo([qs[0]], (2.1, (qs[1], "bar")));
        }
    }

    operation UnusedQubitCirc() : Unit {
        using (qs = Qubit[3]) {
            CNOT(qs[2], qs[0]);
            Reset(qs[0]);
            Reset(qs[2]);
        }
    }

    operation EmptyCirc() : Unit {
        using (qs = Qubit[3]) {
        }
    }

    operation NestedCirc() : Unit {
        using (q = Qubit()) {
            H(q);
            HCirc();
            Reset(q);
        }
    }

    operation FooBar(q : Qubit) : Unit {
        H(q);
        X(q);
        H(q);
    }

    operation Depth2Circ() : Unit {
        using (q = Qubit()) {
            FooBar(q);
        }
    }

    operation PartialOpCirc() : Unit {
        using (qs = Qubit[3]) {
            (Controlled H(qs[0..1], _))(qs[2]);
            ((Ry(_, _))(2.5, _))(qs[0]);
            ResetAll(qs);
        }
    }

    operation Bar((alpha : Double, beta : Double), (q : Qubit, name : String)) : Unit
    is Adj + Ctl {
    }

    operation BigCirc() : Unit {
        using (qs = Qubit[3]) {
            H(qs[0]);
            Ry(2.5, qs[1]);
            Bar((1.0, 2.1), (qs[0], "foo"));
            X(qs[0]);
            CCNOT(qs[0], qs[1], qs[2]);
            Controlled CNOT([qs[0]], (qs[1], qs[2]));
            Controlled Adjoint Bar([qs[2]], ((1.0, 2.1), (qs[0], "foo")));
            let res = M(qs[0]);
            ResetAll(qs);
        }
    }
    
}


