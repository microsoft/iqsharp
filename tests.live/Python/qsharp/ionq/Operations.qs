// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

}
