// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// Q# code should be in one or more .qs files that live 
/// in the same directory as the python classical driver.
///

namespace Microsoft.Quantum.SanityTests {
    open Microsoft.Quantum.Intrinsic;

    /// # Summary
    /// The simplest program. Just generate a debug Message on the console.
    operation HelloQ() : Unit {
        Message($"Hello from quantum world!"); 
    }

    /// # Summary
    /// A more sophisticated program that shows how to 
    /// specify parameters, instantiate qubits, and return values.
    operation HelloAgain(count : Int, name : String) : Result[] {
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

    /// # Summary
    /// Checks that built-in complex types can be used as arguments.
    operation IndexIntoTuple(count : Int, tuples : (Result, String)[]) : (Result, String) {
        return tuples[count];
    }

    /// # Summary
    /// Checks that nested arrays can be used as arguments.
    operation IndexIntoNestedArray(index1 : Int, index2 : Int, nestedArray : Int[][]) : Int {
        return nestedArray[index1][index2];
    }

    /// # Summary
    /// Checks that Result-type arguments are handled correctly.
    operation EchoResult(input : Result) : Result {
        return input;
    }

    /// # Summary
    /// Checks that Pauli-type and arrays of Pauli-type arguments are handled correctly.
    operation SwapFirstPauli(paulis : Pauli[], pauliToSwap : Pauli) : (Pauli[], Pauli) {
        return (paulis w/ 0 <- pauliToSwap, paulis[0]);
    }

    /// # Summary
    /// Checks that a 10-tuple can be round-tripped correctly.
    operation EchoTenTuple(tenTuple : (Int, Int, Int, Int, Int, Int, Int, Int, Int, Int)) : (Int, Int, Int, Int, Int, Int, Int, Int, Int, Int) {
        return tenTuple;
    }

    /// # Summary
    /// Checks that a 10-tuple is processed correctly.
    operation IndexIntoTenTuple(index : Int, tenTuple : (Int, Int, Int, Int, Int, Int, Int, Int, Int, Int)) : Int {
        let (x0, x1, x2, x3, x4, x5, x6, x7, x8, x9) = tenTuple;
        if (index == 0) {
            return x0;
        }
        if (index == 1) {
            return x1;
        }
        if (index == 2) {
           return x2;
        }
        if (index == 3) {
           return x3;
        }
        if (index == 4) {
           return x4;
        }
        if (index == 5) {
           return x5;
        }
        if (index == 6) {
           return x6;
        }
        if (index == 7) {
           return x7;
        }
        if (index == 8) {
           return x8;
        }
        if (index == 9) {
           return x9;
        }
        return -1;
    }

    operation MeasureOne() : Result {
        use q = Qubit();
        X(q);
        return MResetZ(q);
    }
}
