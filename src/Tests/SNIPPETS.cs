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

        public static string Op6b_Op6a =
@"
    /// # Summary
    ///     This to show that the order of snippet operations returned
    ///     from compilation is the same as the order in which they
    ///     are defined in the code:
    operation Op6b() : Unit
    {
        HelloQ();
    }

    operation Op6a() : Unit
    {
        HelloQ();
    }
";

        public static string Op7_EndsWithComment =
@"
    operation Op7() : Unit
    {
        HelloQ();
    } // This snippet ends with a comment";

        public static string CommentOnly = @"// This snippet contains only a comment";

        public static string OpenNamespaces1 =
@"
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Diagnostics;
";

        public static string OpenNamespaces2 =
@"
    open Tests.qss;
    open Microsoft.Quantum.Diagnostics;
";

        public static string DependsOnNamespace =
@"
    operation DependsOnNamespace() : Unit
    {
        using (qubits = Qubit[3])
        {
            Message(""Hello from DependsOnNamespace"");
            HelloQ();
            DumpMachine();
        }
    }
";

        public static string OpenAliasedNamespace =
@"
    open Microsoft.Quantum.Diagnostics as Diag;
";
        
        public static string DependsOnAliasedNamespace =
 @"
    operation DependsOnAliasedNamespace() : Unit
    {
        using (qubits = Qubit[3])
        {
            Message(""Hello from DependsOnAliasedNamespace"");
            Diag.DumpMachine();
        }
    }
";

        public static string SimpleDebugOperation =
 @"
    operation SimpleDebugOperation() : (Result, Result)
    {
        using (qubits = Qubit[2])
        {
            H(qubits[0]);
            H(qubits[1]);
            return (M(qubits[0]), M(qubits[1]));
        }
    }
";

        public static string FailIfOne =
 @"
    operation FailIfOne() : Unit {
        use q = Qubit();
        if M(q) == One {
            fail ""Expected measuring a freshly allocated qubit to return Zero, but returned One."";
        }
    }
";
        public static string ApplyWithinBlock =
 @"
    /// # Summary
    ///     Checks that within/apply block is properly compiled.
    ///     See https://github.com/microsoft/iqsharp/issues/266.
    @EntryPoint()
    operation ApplyWithinBlock() : Unit
    {
        using (q = Qubit())
        {
            within {
                H(q);
                Message(""Within"");
            }
            apply {
                X(q);
                Message(""Apply"");
            }
        }
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

        public static string DumpToFile =
@"
    /// # Summary
    ///     This checks the ability to dump simulator state to a file.
    operation DumpToFile() : Unit
    {
        using (qs = Qubit[2])
        {
            Microsoft.Quantum.Diagnostics.DumpMachine(""DumpMachine.txt"");
            Microsoft.Quantum.Diagnostics.DumpRegister(""DumpRegister.txt"", qs);
            Message(""Dumped to file!"");
        }
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
    open Tests.IQSharp.Chemistry.Samples;
    open Mock.Chemistry;

    operation DependsOnChemistryWorkspace() : ((JordanWignerEncodingData, Int, Double) => (Double, Double))
    {
        return UseJordanWignerEncodingData;
    }
";

        public static string UseJordanWignerEncodingData =
@"
    open Mock.Chemistry;
    
    operation UseJordanWignerEncodingData (qSharpData: JordanWignerEncodingData, nBitsPrecision : Int, trotterStepSize : Double) : (Double, Double) {        
        let (nSpinOrbitals, data, statePrepData, energyShift) = qSharpData!;
                
        // Prepare ProductState
        let estPhase = 2.0;
        let estEnergy = 3.0;

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

        public static string InvalidEntryPoint =
@"
    /// # Summary
    ///     This script has an operation that is not valid to be marked as an entry point.
    operation InvalidEntryPoint(q : Qubit) : Unit {
        H(q);
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

        public static string CompareMeasurementResult =
@"
    operation CompareMeasurementResult() : Result {
        using (qubits = Qubit[2]) {
            let r = M(qubits[0]);
            if (r == One) {
                H(qubits[1]);
                Reset(qubits[1]);
            }
            return r;
        }
    }
";

    }
}
