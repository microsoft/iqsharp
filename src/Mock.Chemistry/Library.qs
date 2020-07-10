// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


///
/// These are mock classes that resemble the data structures defined in the chemistry library
///
namespace Mock.Chemistry {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Canon;
    
    
    /// # Summary
    /// Format of data passed from C# to Q# to represent a term of the Hamiltonian.
    /// The meaning of the data represented is determined by the algorithm that receives it.
    newtype HTerm = (Int[], Double[]);

    /// # Summary
    /// Format of data passed from C# to Q# to represent terms of the Hamiltonian.
    /// The meaning of the data represented is determined by the algorithm that receives it.
    newtype JWOptimizedHTerms = (HTerm[], HTerm[], HTerm[], HTerm[]);
    
    /// # Summary
    /// Format of data passed from C# to Q# to represent preparation of the initial state
    /// The meaning of the data represented is determined by the algorithm that receives it.
    newtype JordanWignerInputState = ((Double, Double), Int[]);
    
    /// # Summary
    /// Format of data passed from C# to Q# to represent all information for Hamiltonian simulation.
    /// The meaning of the data represented is determined by the algorithm that receives it.
    newtype JordanWignerEncodingData = (Int, JWOptimizedHTerms, (Int, JordanWignerInputState[]), Double);
}