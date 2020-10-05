// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Tests.ExecutionPathTracer {
    
    open Microsoft.Quantum.Intrinsic;

    // Custom operation
    operation Foo(theta : Double, (qubit : Qubit, bar : String)) : Unit
    is Adj + Ctl { }

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

    operation NoQubitCirc(n : Int) : Unit { }

    operation NoQubitArgsCirc() : Unit {
        NoQubitCirc(2);
    }

    operation WithQArrayArgs(bits: Bool[]): Unit { }

    operation WithQArrayArgsCirc(): Unit {
        WithQArrayArgs([false, true]);
    }

    operation OperationCirc(op : (Qubit => Unit), n : Int) : Unit { }

    operation OperationArgsCirc() : Unit {
        OperationCirc(H, 5);
    }

    operation NestedCirc() : Unit {
        using (q = Qubit()) {
            H(q);
            HCirc();
            Reset(q);
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
    is Adj + Ctl { }

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
