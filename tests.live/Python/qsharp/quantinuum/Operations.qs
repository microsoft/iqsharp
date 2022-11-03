// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Tests {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Canon;

    operation RunTeleportWithPlus() : Result {
        return RunTeleport(true);
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
