// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//
// These are mock classes that resemble the data structures defined in the Microsoft.Quantum.Arrays library
//
namespace Mock.Standard {
    open Microsoft.Quantum.Arrays;

    /// # Summary
    /// Given an array and an operation that is defined
    /// for the elements of the array, returns a new array that consists
    /// of the images of the original array under the operation.
    ///
    /// # Remarks
    /// The operation is defined for generic types, i.e., whenever we have
    /// an array `'T[]` and an operation `action : 'T -> 'U` we can map the elements
    /// of the array and produce a new array of type `'U[]`.
    ///
    /// # Type Parameters
    /// ## 'T
    /// The type of `array` elements.
    /// ## 'U
    /// The result type of the `action` operation.
    ///
    /// # Input
    /// ## action
    /// An operation from `'T` to `'U` that is applied to each element.
    /// ## array
    /// An array of elements over `'T`.
    ///
    /// # Output
    /// An array `'U[]` of elements that are mapped by the `action` operation.
    operation ForEach<'T, 'U> (action : ('T => 'U), array : 'T[]) : 'U[] {
        mutable resultArray = new 'U[Length(array)];

        for idxElement in IndexRange(array) {
            set resultArray w/= idxElement <- action(array[idxElement]);
        }

        return resultArray;
    }
}
