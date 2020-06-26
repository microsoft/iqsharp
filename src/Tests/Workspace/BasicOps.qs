// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Tests.qss {
    
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Canon;

    // Note that we specify multiple @EntryPoint() operations in this file
    // to verify that these attributes have no impact on the compilation of
    // the file for use in IQ#.
    
    /// # Summary: 
    ///     The simplest program. Just generate a debug Message on the console.
    @EntryPoint()
    operation HelloQ() : Unit
    {
        Message($"Hello from quantum world!"); 
    }

    /// # Summary: 
    ///     A more sophisticated program that shows how to 
    ///     specify parameters, instantiate qubits, and return values.
    @EntryPoint()
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
            ApplyIf(T, applyT, qubits[0]);
        } 
    }
}


