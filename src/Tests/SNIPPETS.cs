// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Tests.IQSharp
{
    public static class SNIPPETS
    {
        public static string HelloQ =
@"
    /// # Summary
    ///     The simplest program. Just generate a debug Message on the console.
    operation HelloQ() : Unit
    {
        Message($""Hello from quantum world!""); 
    }
";

        public static string HelloQ_2 =
@"
    /// # Summary
    ///     This to show that you can override the definition:
    operation HelloQ() : Unit
    {
        Message($""msg0""); 
        Message($""msg1""); 
    }
";
        public static string DependsOnHelloQ =
@"
    /// # Summary
    ///     This to show that you can depend on other snippets:
    operation DependsOnHelloQ() : Unit
    {
        HelloQ();
    }
";

        public static string Op1_Op2 =
@"
    /// # Summary
    ///     This to show that you can define two operations in the same snippet:
    operation Op1() : Unit
    {
        HelloQ();
    }

    operation Op2() : Unit
    {
        Op1();
    }
";

        public static string Op3_Op4_Op5_EntryPoints =
@"
    /// # Summary
    ///     This to show that @EntryPoint() is ignored when compiling from snippets:
    @EntryPoint()
    operation Op3() : Unit
    {
        HelloQ();
    }

    @EntryPoint()
    operation Op4() : Unit
    {
        Op3();
    }

    @ EntryPoint (  )
    operation Op5() : Unit
    {
        Op4();
    }
";

        public static string DependsOnWorkspace =
@"
    /// # Summary
    ///     This to check that you can call operations from the workspace:
    operation DependsOnWorkspace() : Result[]
    {
        return Tests.qss.HelloAgain(5, ""Foo"");
    }
";


        public static string OneWarning =
@"

    /// # Summary
    ///     This script has one warning for using `()`
    operation OneWarning() : ()
    {
        Message($""msg0""); 
    }
";

        public static string ThreeWarnings =
@"

    /// # Summary
    ///     This script has two warnings. One for using `()`, and two 
    ///     for missing `(...)`
    operation ThreeWarnings() : ()
    {
        body {
            Message($""msg0""); 
            Message($""msg1""); 
            Message($""msg2""); 
        }

        adjoint {}
    }
";

        public static string TwoErrors =
@"

    /// # Summary
    ///     This script has errors:
    ///     * Unknown UDT (header)
    ///     * Missing semicolon
    operation TwoErrors(foo: Bar) : ()
    {
        body {
            Message($""msg0""); 
            Message(""msg1"")           // Missing semicolon 
            Message($""msg2""); 
        }
    }
";

        public static string DependsOnChemistryWorkspace =
@"    
    /// # Summary
    ///     This scripts depend on the Chemistry.Workspace correctly loaded.
    ///     
    open Microsoft.Quantum.Chemistry.Samples;
    open Microsoft.Quantum.Chemistry.JordanWigner; 

    operation DependsOnChemistryWorkspace() : ((JordanWignerEncodingData, Int, Double) => (Double, Double))
    {
        return TrotterEstimateEnergy;
    }
";

        public static string TrotterEstimateEnergy =
@"

    /// # Summary
    ///     This script depends on the Chemistry package to compile.
    
    open Microsoft.Quantum.Chemistry.JordanWigner;    
    open Microsoft.Quantum.Characterization;
    open Microsoft.Quantum.Simulation;
    
    operation TrotterEstimateEnergy (qSharpData: JordanWignerEncodingData, nBitsPrecision : Int, trotterStepSize : Double) : (Double, Double) {
        
        let (nSpinOrbitals, data, statePrepData, energyShift) = qSharpData!;
        
        // Order of integrator
        let trotterOrder = 1;
        let (nQubits, (rescaleFactor, oracle)) = TrotterStepOracle(qSharpData, trotterStepSize, trotterOrder);
        
        // Prepare ProductState
        let statePrep =  PrepareTrialState(statePrepData, _);
        let phaseEstAlgorithm = RobustPhaseEstimation(nBitsPrecision, _, _);
        let estPhase = EstimateEnergy(nQubits, statePrep, oracle, phaseEstAlgorithm);
        let estEnergy = estPhase * rescaleFactor + energyShift;
        return (estPhase, estEnergy);
    }
";

        public static string InvalidFunctor =
@"
    /// # Summary
    ///     This script has an operation can't have adjoint since it has a measurement inside.
    operation InvalidFunctor(q: Qubit) : Unit {
        body(...) {
            let m = M(q);

            if (m) { X(q); }
        }

        adjoint auto;
    }
";

        public static string Reverse =
@"
    /// # Summary
    ///     This script returns the same array in reverse order.
    ///     Used to make sure we can pass arguments to snippets simulation.
        operation Reverse(name : String, array: Int[]) : Int[] {
         Message($""Hello {name}"");
         
        let n = Length(array);
        mutable m = new Int[n];

        for(i in n-1..-1..0) {
            set m w/= i <- array[n - 1 - i];
        }
        
        return m;
    }
";


    }
}
