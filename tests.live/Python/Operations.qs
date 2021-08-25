// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// Q# code should be in one or more .qs files that live 
/// in the same directory as the python classical driver.
///

namespace Microsoft.Quantum.SanityTests {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Canon;

    /// # Summary
    /// A quantum random number generator with a variable number of qubits.
    operation QRNG(count : Int, name : String) : Result[] {
        Message($"Hello {name} again!");

        mutable r = [Zero, size = count];
        use q = Qubit[count];

        ApplyToEach(H, q);
        
        for i in 1..count {
            set r w/= i-1 <- M(q[i-1]);
        }

        return r;
    }
}
