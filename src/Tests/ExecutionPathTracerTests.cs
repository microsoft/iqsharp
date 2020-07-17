// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.Simulation.Simulators;
using Microsoft.Quantum.IQSharp.Core.ExecutionPathTracer;

namespace Tests.IQSharp
{
    [TestClass]
    public class IntrinsicTests
    {
        public Workspace InitWorkspace()
        {
            var ws = Startup.Create<Workspace>("Workspace.ExecutionPathTracer");
            ws.Reload();
            Assert.IsFalse(ws.HasErrors);
            return ws;
        }

        public ExecutionPath GetExecutionPath(string name, int depth = 1)
        {
            var ws = InitWorkspace();
            var op = ws.AssemblyInfo.Operations.SingleOrDefault(o => o.FullName == $"Tests.ExecutionPathTracer.{name}");
            Assert.IsNotNull(op);

            var tracer = new ExecutionPathTracer(depth);
            using var qsim = new QuantumSimulator().WithExecutionPathTracer(tracer);
            op.RunAsync(qsim, new Dictionary<string, string>()).Wait();

            return tracer.GetExecutionPath();
        }


        [TestMethod]
        public void HTest()
        {
            var path = GetExecutionPath("HCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "H",
                    Targets = new List<Register>() { new QubitRegister(0) },
                },
                // TODO: Remove Reset/ResetAll gates once we don't need to zero out qubits
                new Operation()
                {
                    Gate = "Reset",
                    Targets = new List<Register>() { new QubitRegister(0) },
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            Assert.AreEqual(expected.ToJson(), path.ToJson());
        }

        [TestMethod]
        public void MTest()
        {
            var path = GetExecutionPath("MCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0, 1),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "measure",
                    Controls = new List<Register>() { new QubitRegister(0) },
                    Targets = new List<Register>() { new ClassicalRegister(0, 0) },
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            Assert.AreEqual(expected.ToJson(), path.ToJson());
        }

        [TestMethod]
        public void CnotTest()
        {
            var path = GetExecutionPath("CnotCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0),
                new QubitDeclaration(1),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "X",
                    Controlled = true,
                    Controls = new List<Register>() { new QubitRegister(0) },
                    Targets = new List<Register>() { new QubitRegister(1) },
                },
                new Operation()
                {
                    Gate = "ResetAll",
                    Targets = new List<Register>() { new QubitRegister(0), new QubitRegister(1) },
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            Assert.AreEqual(expected.ToJson(), path.ToJson());
        }

        [TestMethod]
        public void CcnotTest()
        {
            var path = GetExecutionPath("CcnotCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0),
                new QubitDeclaration(1),
                new QubitDeclaration(2),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "X",
                    Controlled = true,
                    Controls = new List<Register>() { new QubitRegister(0), new QubitRegister(2) },
                    Targets = new List<Register>() { new QubitRegister(1) },
                },
                new Operation()
                {
                    Gate = "ResetAll",
                    Targets = new List<Register>()
                    {
                        new QubitRegister(0),
                        new QubitRegister(1),
                        new QubitRegister(2),
                    },
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            Assert.AreEqual(expected.ToJson(), path.ToJson());
        }

        [TestMethod]
        public void SwapTest()
        {
            var path = GetExecutionPath("SwapCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0),
                new QubitDeclaration(1),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "SWAP",
                    Targets = new List<Register>() { new QubitRegister(0), new QubitRegister(1) },
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            Assert.AreEqual(expected.ToJson(), path.ToJson());
        }

        [TestMethod]
        public void RxTest()
        {
            var path = GetExecutionPath("RxCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "Rx",
                    DisplayArgs = "(2)",
                    Targets = new List<Register>() { new QubitRegister(0) },
                },
                new Operation()
                {
                    Gate = "Reset",
                    Targets = new List<Register>() { new QubitRegister(0) },
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            Assert.AreEqual(expected.ToJson(), path.ToJson());
        }

        [TestMethod]
        public void AdjointHTest()
        {
            var path = GetExecutionPath("AdjointHCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "H",
                    Adjoint = true,
                    Targets = new List<Register>() { new QubitRegister(0) },
                },
                new Operation()
                {
                    Gate = "Reset",
                    Targets = new List<Register>() { new QubitRegister(0) },
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            Assert.AreEqual(expected.ToJson(), path.ToJson());
        }

        [TestMethod]
        public void ControlledXTest()
        {
            var path = GetExecutionPath("ControlledXCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0),
                new QubitDeclaration(1),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "X",
                    Controlled = true,
                    Controls = new List<Register>() { new QubitRegister(0) },
                    Targets = new List<Register>() { new QubitRegister(1) },
                },
                new Operation()
                {
                    Gate = "ResetAll",
                    Targets = new List<Register>() { new QubitRegister(0), new QubitRegister(1) },
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            Assert.AreEqual(expected.ToJson(), path.ToJson());

            // JSON should be the same as CNOT's
            var path2 = GetExecutionPath("CnotCirc");
            Assert.AreEqual(path.ToJson(), path2.ToJson());
        }

        [TestMethod]
        public void ControlledAdjointSTest()
        {
            var path = GetExecutionPath("ControlledAdjointSCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0),
                new QubitDeclaration(1),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "S",
                    Controlled = true,
                    Adjoint = true,
                    Controls = new List<Register>() { new QubitRegister(0) },
                    Targets = new List<Register>() { new QubitRegister(1) },
                },
                new Operation()
                {
                    Gate = "ResetAll",
                    Targets = new List<Register>() { new QubitRegister(0), new QubitRegister(1) },
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            Assert.AreEqual(expected.ToJson(), path.ToJson());
        }

        [TestMethod]
        public void FooTest()
        {
            var path = GetExecutionPath("FooCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "Foo",
                    DisplayArgs = "(2.1, (\"bar\"))",
                    Targets = new List<Register>() { new QubitRegister(0) },
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            Assert.AreEqual(expected.ToJson(), path.ToJson());
        }

        [TestMethod]
        public void ControlledFooTest()
        {
            var path = GetExecutionPath("ControlledFooCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0),
                new QubitDeclaration(1),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "Foo",
                    DisplayArgs = "(2.1, (\"bar\"))",
                    Controlled = true,
                    Controls = new List<Register>() { new QubitRegister(0) },
                    Targets = new List<Register>() { new QubitRegister(1) },
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            Assert.AreEqual(expected.ToJson(), path.ToJson());
        }

        [TestMethod]
        public void UnusedQubitTest()
        {
            var path = GetExecutionPath("UnusedQubitCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0),
                new QubitDeclaration(2),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "X",
                    Controlled = true,
                    Controls = new List<Register>() { new QubitRegister(2) },
                    Targets = new List<Register>() { new QubitRegister(0) },
                },
                new Operation()
                {
                    Gate = "Reset",
                    Targets = new List<Register>() { new QubitRegister(0) },
                },
                new Operation()
                {
                    Gate = "Reset",
                    Targets = new List<Register>() { new QubitRegister(2) },
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            Assert.AreEqual(expected.ToJson(), path.ToJson());
        }

        [TestMethod]
        public void Depth2Test()
        {
            var path = GetExecutionPath("Depth2Circ", 2);
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0, 1),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "H",
                    Targets = new List<Register>() { new QubitRegister(0) },
                },
                new Operation()
                {
                    Gate = "X",
                    Targets = new List<Register>() { new QubitRegister(0) },
                },
                new Operation()
                {
                    Gate = "H",
                    Targets = new List<Register>() { new QubitRegister(0) },
                },
                new Operation()
                {
                    Gate = "measure",
                    Controls = new List<Register>() { new QubitRegister(0) },
                    Targets = new List<Register>() { new ClassicalRegister(0, 0) },
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            Assert.AreEqual(expected.ToJson(), path.ToJson());
        }

        [TestMethod]
        public void PartialOpTest()
        {
            var path = GetExecutionPath("PartialOpCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0),
                new QubitDeclaration(1),
                new QubitDeclaration(2),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "H",
                    Controlled = true,
                    Controls = new List<Register>() { new QubitRegister(0), new QubitRegister(1) },
                    Targets = new List<Register>() { new QubitRegister(2) },
                },
                new Operation()
                {
                    Gate = "Ry",
                    DisplayArgs = "(2.5)",
                    Targets = new List<Register>() { new QubitRegister(0) },
                },
                new Operation()
                {
                    Gate = "ResetAll",
                    Targets = new List<Register>() {
                        new QubitRegister(0),
                        new QubitRegister(1),
                        new QubitRegister(2),
                    },
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            Assert.AreEqual(expected.ToJson(), path.ToJson());
        }

        [TestMethod]
        public void EmptyTest()
        {
            var path = GetExecutionPath("EmptyCirc");
            var qubits = new QubitDeclaration[] { };
            var operations = new Operation[] { };
            var expected = new ExecutionPath(qubits, operations);
            Assert.AreEqual(expected.ToJson(), path.ToJson());
        }

        [TestMethod]
        public void BigTest()
        {
            var path = GetExecutionPath("BigCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0, 1),
                new QubitDeclaration(1),
                new QubitDeclaration(2),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "H",
                    Targets = new List<Register>() { new QubitRegister(0) },
                },
                new Operation()
                {
                    Gate = "Ry",
                    DisplayArgs = "(2.5)",
                    Targets = new List<Register>() { new QubitRegister(1) },
                },
                new Operation()
                {
                    Gate = "Bar",
                    DisplayArgs = "((1, 2.1), (\"foo\"))",
                    Targets = new List<Register>() { new QubitRegister(0) },
                },
                new Operation()
                {
                    Gate = "X",
                    Targets = new List<Register>() { new QubitRegister(0) },
                },
                new Operation()
                {
                    Gate = "X",
                    Controlled = true,
                    Controls = new List<Register>() { new QubitRegister(0), new QubitRegister(1) },
                    Targets = new List<Register>() { new QubitRegister(2) },
                },
                new Operation()
                {
                    Gate = "X",
                    Controlled = true,
                    Controls = new List<Register>() { new QubitRegister(0), new QubitRegister(1) },
                    Targets = new List<Register>() { new QubitRegister(2) },
                },
                new Operation()
                {
                    Gate = "Bar",
                    DisplayArgs = "((1, 2.1), (\"foo\"))",
                    Controlled = true,
                    Adjoint = true,
                    Controls = new List<Register>() { new QubitRegister(2) },
                    Targets = new List<Register>() { new QubitRegister(0) },
                },
                new Operation()
                {
                    Gate = "measure",
                    Controls = new List<Register>() { new QubitRegister(0) },
                    Targets = new List<Register>() { new ClassicalRegister(0, 0) },
                },
                new Operation()
                {
                    Gate = "ResetAll",
                    Targets = new List<Register>() {
                        new QubitRegister(0),
                        new QubitRegister(1),
                        new QubitRegister(2),
                    },
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            Assert.AreEqual(expected.ToJson(), path.ToJson());
        }
    }
}
