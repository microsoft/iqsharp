// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// Q# code should be in one or more .qs files that live 
/// in the same directory as the python classical driver.
///

namespace Microsoft.Quantum.Tests {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Canon;

    /// # Summary
    /// A quantum random number generator with a variable number of qubits.
    operation SampleQrng(count : Int, name : String) : Result[] {
        Message($"Hello {name} again!");

        mutable r = [Zero, size = count];
        use q = Qubit[count];

        ApplyToEach(H, q);
        
        for i in 1..count {
            set r w/= i-1 <- M(q[i-1]);
        }

        return r;
    }

    
    /// # Summary
    /// Direct implementation of Teleport's circuit
    operation RunTeleport(doPlus: Bool) : Result {
        Message("Running teleport circuit");
        
        use qubits = Qubit[3];
        // Entangle
        H(qubits[1]);
        CNOT(qubits[1], qubits[2]);

        // Encode
        if (doPlus) {
            SetPlus(qubits[0]);
        } else {
            SetMinus(qubits[0]);
        }
        
        CNOT(qubits[0], qubits[1]);
        H(qubits[0]);
        let classicInfo = M(qubits[0]);

        // Decode
        if (M(qubits[1]) == One) { X(qubits[2]); }
        if (classicInfo == One)  { Z(qubits[2]); }

        // Report message received:
        return Measure([PauliX], qubits[2..2]);
    }
    
    /// # Summary
    /// Sets the qubit's state to |+>
    operation SetPlus(q: Qubit) : Unit {
        H(q);
    }
    
    /// # Summary
    /// Sets the qubit's state to |->
    operation SetMinus(q: Qubit) : Unit {
        X(q);
        H(q);
    }
}
