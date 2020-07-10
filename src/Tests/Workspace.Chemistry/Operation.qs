// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// This file can only compile if the Mock.Chemistry project
// is added as a reference.
namespace Tests.IQSharp.Chemistry.Samples {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Math;
    open Mock.Chemistry;
    
    operation UseJordanWignerEncodingData (qSharpData: JordanWignerEncodingData, nBitsPrecision : Int, trotterStepSize : Double) : (Double, Double) {        
        let (nSpinOrbitals, data, statePrepData, energyShift) = qSharpData!;
                
        // Prepare ProductState
        let estPhase = 2.0;
        let estEnergy = 3.0;

        return (estPhase, estEnergy);
    }

    
    operation UseHTerm (qSharpData: HTerm[]) : Unit {        
    }
}


