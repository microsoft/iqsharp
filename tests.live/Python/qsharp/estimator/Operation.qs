// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Tests {
    open Microsoft.Quantum.Arithmetic;
    
    operation EstimateMultiplication(bitwidth : Int) : Unit {
        use factor1 = Qubit[bitwidth];
        use factor2 = Qubit[bitwidth];
        use product = Qubit[2 * bitwidth];

        MultiplyI(LittleEndian(factor1), LittleEndian(factor2), LittleEndian(product));
    }
}
