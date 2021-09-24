// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Tests.qss {
    open Microsoft.Quantum.Intrinsic;
    //[ERROR]: MISSING Microsoft.Quantum.Standard;

    /// # Summary
    /// The simplest program. Just generate a debug Message on the console.
    operation HelloQ() : Unit
    {
        Message($"Hello from quantum world!"); 
    }

    /// # Summary: 
    ///     A more sophisticated program that shows how to 
    ///     specify parameters, instantiate qubits, and return values.
    operation HelloAgain(count: Int, name: String) : Result[]
    {
        Message($"Hello {name} again!"); 

        mutable r = new Result[count];
        using (q = Qubit()) {
            for (i in 1..count) {
                if (i == 2) { X(q); }
                set r w/= i-1 <- M(q);
                Reset(q);
            }
        }

        return r;
    }
    
    operation CCNOTDriver(applyT : Bool) : Unit {
        using(qubits = Qubit[3]) {
            CCNOT(qubits[0], qubits[1], qubits[2]);
            ApplyIf(applyT, T, qubits[0]);
        } 
    }
}


