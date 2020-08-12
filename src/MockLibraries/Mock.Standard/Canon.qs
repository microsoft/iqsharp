// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//
// These are mock classes that resemble the data structures defined in the Microsoft.Quantum.Canon library
//
namespace Mock.Standard {
    open Microsoft.Quantum.Arrays;

    /// # Summary
    /// Applies a single-qubit operation to each element in a register.
    ///
    /// # Input
    /// ## singleElementOperation
    /// Operation to apply to each qubit.
    /// ## register
    /// Array of qubits on which to apply the given operation.
    ///
    /// # Type Parameters
    /// ## 'T
    /// The target on which the operation acts.
    ///
    /// # Remarks
    /// ## Example
    /// Prepare a three-qubit $\ket{+}$ state:
    /// ```qsharp
    /// using (register = Qubit[3]) {
    ///     ApplyToEach(H, register);
    /// }
    /// ```
    ///
    /// # See Also
    /// - Microsoft.Quantum.Canon.ApplyToEachC
    /// - Microsoft.Quantum.Canon.ApplyToEachA
    /// - Microsoft.Quantum.Canon.ApplyToEachCA
    operation ApplyToEach<'T> (singleElementOperation : ('T => Unit), register : 'T[]) : Unit
    {
        for (idxQubit in IndexRange(register))
        {
            singleElementOperation(register[idxQubit]);
        }
    }
}
