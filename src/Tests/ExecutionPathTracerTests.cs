// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.ExecutionPathTracer;
using Microsoft.Quantum.Simulation.Simulators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.IQSharp
{
    public class ExecutionPathTracerTests
    {
        IEnumerable<OperationInfo>? operations = null;
        public Workspace InitWorkspace()
        {
            var ws = Startup.Create<Workspace>("Workspace.ExecutionPathTracer");
            ws.GlobalReferences.AddPackage("mock.standard").Wait();
            ws.Reload();
            Assert.IsFalse(ws.HasErrors);
            return ws;
        }

        public ExecutionPath GetExecutionPath(string name)
        {
            if (this.operations == null)
            {
                var ws = InitWorkspace();
                this.operations = ws.AssemblyInfo.Operations;
            }

            var op = this.operations.SingleOrDefault(o => o.FullName == $"Tests.ExecutionPathTracer.{name}");
            Assert.IsNotNull(op);

            var tracer = new ExecutionPathTracer();
            using var qsim = new QuantumSimulator().WithExecutionPathTracer(tracer);
            op.RunAsync(qsim, new Dictionary<string, string>()).Wait();

            return tracer.GetExecutionPath();
        }

        public void AssertExecutionPathsEqual(ExecutionPath expected, ExecutionPath actual)
        {
            // Step in one depth lower
            actual = new ExecutionPath(actual.Qubits, actual.Operations.First().Children?.ToList() ?? new List<Operation>());
            // Prune non-deterministic gates as it's difficult to test
            PruneNonDeterministicGates(actual.Operations);
            Assert.AreEqual(expected.ToJson(), actual.ToJson());
        }

        private void PruneNonDeterministicGates(IEnumerable<Operation>? operations)
        {
            if (operations == null) return;
            foreach (var op in operations)
            {
                // Remove all measurements within Reset gates and composite
                // measurements (non-deterministic)
                if (op.Gate == "Reset" || op.IsMeasurement) op.Children = null;
                else PruneNonDeterministicGates(op.Children);
            }
        }

        // Helper functions for tests
        public Operation ControlledX(int[] controlId, int targetId) =>
            new Operation()
            {
                Gate = "X",
                IsControlled = true,
                Controls = controlId.Select(id => new QubitRegister(id)),
                Targets = new List<Register>() { new QubitRegister(targetId) },
            };

        public Operation Reset(int qId) =>
            new Operation()
            {
                Gate = "Reset",
                Targets = new List<Register>() { new QubitRegister(qId) },
            };

        public Operation ResetAll(int[] qIds) =>
            new Operation()
            {
                Gate = "ResetAll",
                Targets = qIds.Select(id => new QubitRegister(id)),
                Children = ImmutableList<Operation>.Empty.AddRange(qIds.Select(id => Reset(id))),
            };
    }

    [TestClass]
    public class IntrinsicTests : ExecutionPathTracerTests
    {
        [TestMethod]
        public void HTest()
        {
            var path = GetExecutionPath("HCirc");
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
                Reset(0),
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
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
                    Gate = "M",
                    IsMeasurement = true,
                    Controls = new List<Register>() { new QubitRegister(0) },
                    Targets = new List<Register>() { new ClassicalRegister(0, 0) },
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }

        [TestMethod]
        public void CnotTest()
        {
            var path = GetExecutionPath("CnotCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0, 1),
                new QubitDeclaration(1, 1),
            };
            var operations = new Operation[]
            {
                ControlledX(new int[] { 0 }, 1),
                ResetAll(new int[]{ 0, 1 }),
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }

        [TestMethod]
        public void CcnotTest()
        {
            var path = GetExecutionPath("CcnotCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0, 1),
                new QubitDeclaration(1, 1),
                new QubitDeclaration(2, 1),
            };
            var operations = new Operation[]
            {
                ControlledX(new int[] { 0, 2 }, 1),
                ResetAll(new int[]{ 0, 1, 2 }),
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
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
                    Children = ImmutableList<Operation>.Empty.AddRange(
                        new [] {
                            ControlledX(new int[] { 0 }, 1),
                            ControlledX(new int[] { 1 }, 0),
                            ControlledX(new int[] { 0 }, 1),
                        }
                    )
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }

        [TestMethod]
        public void RxTest()
        {
            var path = GetExecutionPath("RxCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0, 1),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "Rx",
                    DisplayArgs = "(2)",
                    Targets = new List<Register>() { new QubitRegister(0) },
                    Children = ImmutableList<Operation>.Empty.AddRange(
                        new [] {
                            new Operation()
                            {
                                Gate = "R",
                                DisplayArgs = "(PauliX, 2)",
                                Targets = new List<Register>() { new QubitRegister(0) },
                            }
                        }
                    )
                },
                Reset(0),
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }

        [TestMethod]
        public void AdjointHTest()
        {
            var path = GetExecutionPath("AdjointHCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0, 1),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "H",
                    IsAdjoint = true,
                    Targets = new List<Register>() { new QubitRegister(0) },
                },
                Reset(0),
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }

        [TestMethod]
        public void ControlledXTest()
        {
            var path = GetExecutionPath("ControlledXCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0, 1),
                new QubitDeclaration(1, 1),
            };
            var operations = new Operation[]
            {
                ControlledX(new int[] { 0 }, 1),
                ResetAll(new int[]{ 0, 1 }),
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);

            // JSON should be the same as CNOT's
            var path2 = GetExecutionPath("CnotCirc");
            AssertExecutionPathsEqual(expected, path2);
        }

        [TestMethod]
        public void ControlledAdjointSTest()
        {
            var path = GetExecutionPath("ControlledAdjointSCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0, 1),
                new QubitDeclaration(1, 1),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "S",
                    IsControlled = true,
                    IsAdjoint = true,
                    Controls = new List<Register>() { new QubitRegister(0) },
                    Targets = new List<Register>() { new QubitRegister(1) },
                },
                ResetAll(new int[]{ 0, 1 }),
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }
    }

    [TestClass]
    public class Circuits : ExecutionPathTracerTests
    {
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
            AssertExecutionPathsEqual(expected, path);
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
                    IsControlled = true,
                    Controls = new List<Register>() { new QubitRegister(0) },
                    Targets = new List<Register>() { new QubitRegister(1) },
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }

        [TestMethod]
        public void UnusedQubitTest()
        {
            var path = GetExecutionPath("UnusedQubitCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0, 1),
                new QubitDeclaration(2, 1),
            };
            var operations = new Operation[]
            {
                ControlledX(new int[] { 2 }, 0),
                Reset(0),
                Reset(2),
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }

        [TestMethod]
        public void PartialOpTest()
        {
            var path = GetExecutionPath("PartialOpCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0, 1),
                new QubitDeclaration(1, 1),
                new QubitDeclaration(2, 1),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "H",
                    IsControlled = true,
                    Controls = new List<Register>() { new QubitRegister(0), new QubitRegister(1) },
                    Targets = new List<Register>() { new QubitRegister(2) },
                },
                new Operation()
                {
                    Gate = "Ry",
                    DisplayArgs = "(2.5)",
                    Targets = new List<Register>() { new QubitRegister(0) },
                    Children = ImmutableList<Operation>.Empty.AddRange(
                        new [] {
                            new Operation()
                            {
                                Gate = "R",
                                DisplayArgs = "(PauliY, 2.5)",
                                Targets = new List<Register>() { new QubitRegister(0) },
                            }
                        }
                    )
                },
                ResetAll(new int[] { 0, 1, 2 }),
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }

        [TestMethod]
        public void EmptyTest()
        {
            var path = GetExecutionPath("EmptyCirc");
            var qubits = new QubitDeclaration[] { };
            var operations = new Operation[] { };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }

        [TestMethod]
        public void NoQubitArgsTest()
        {
            var path = GetExecutionPath("NoQubitArgsCirc");
            var qubits = new QubitDeclaration[] { };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "NoQubitCirc",
                    DisplayArgs = "(2)",
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }

        [TestMethod]
        public void WithQArrayArgsTest()
        {
            var path = GetExecutionPath("WithQArrayArgsCirc");
            var qubits = new QubitDeclaration[] { };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "WithQArrayArgs",
                    DisplayArgs = "([False, True])",
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }

        [TestMethod]
        public void OperationArgsTest()
        {
            var path = GetExecutionPath("OperationArgsCirc");
            var qubits = new QubitDeclaration[] { };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "OperationCirc",
                    DisplayArgs = "(H, 5)",
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }

        [TestMethod]
        public void NestedTest()
        {
            var path = GetExecutionPath("NestedCirc");
            var qubits = new QubitDeclaration[] {
                new QubitDeclaration(0, 1),
                new QubitDeclaration(1, 1),
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
                    Gate = "HCirc",
                    Targets = new List<Register>() { new QubitRegister(1) },
                    Children = ImmutableList<Operation>.Empty.AddRange(
                        new [] {
                            new Operation()
                            {
                                Gate = "H",
                                Targets = new List<Register>() { new QubitRegister(1) },
                            },
                            Reset(1),
                        }
                    )
                },
                Reset(0),
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }

        [TestMethod]
        public void BigTest()
        {
            var path = GetExecutionPath("BigCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0, 2),
                new QubitDeclaration(1, 1),
                new QubitDeclaration(2, 1),
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
                    Children = ImmutableList<Operation>.Empty.AddRange(
                        new [] {
                            new Operation()
                            {
                                Gate = "R",
                                DisplayArgs = "(PauliY, 2.5)",
                                Targets = new List<Register>() { new QubitRegister(1) },
                            },
                        }
                    )
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
                ControlledX(new int[] { 0, 1 }, 2),
                ControlledX(new int[] { 0, 1 }, 2),
                new Operation()
                {
                    Gate = "Bar",
                    DisplayArgs = "((1, 2.1), (\"foo\"))",
                    IsControlled = true,
                    IsAdjoint = true,
                    Controls = new List<Register>() { new QubitRegister(2) },
                    Targets = new List<Register>() { new QubitRegister(0) },
                },
                new Operation()
                {
                    Gate = "M",
                    IsMeasurement = true,
                    Controls = new List<Register>() { new QubitRegister(0) },
                    Targets = new List<Register>() { new ClassicalRegister(0, 0) },
                },
                ResetAll(new int[] { 0, 1, 2 }),
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }
    }

    [TestClass]
    public class CanonTests : ExecutionPathTracerTests
    {
        [TestMethod]
        public void ApplyToEachTest()
        {
            var path = GetExecutionPath("ApplyToEachCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0, 1),
                new QubitDeclaration(1, 1),
                new QubitDeclaration(2, 1),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "ApplyToEach",
                    DisplayArgs = "(H)",
                    Targets = new List<Register>()
                    {
                        new QubitRegister(0),
                        new QubitRegister(1),
                        new QubitRegister(2),
                    },
                    Children = ImmutableList<Operation>.Empty.AddRange(
                        new [] {
                            new Operation()
                            {
                                Gate = "H",
                                Targets = new List<Register>() { new QubitRegister(0) },
                            },
                            new Operation()
                            {
                                Gate = "H",
                                Targets = new List<Register>() { new QubitRegister(1) },
                            },
                            new Operation()
                            {
                                Gate = "H",
                                Targets = new List<Register>() { new QubitRegister(2) },
                            },
                        }
                    )
                },
                ResetAll(new int[] { 0, 1, 2 }),
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }

        [TestMethod]
        public void ApplyToEachDepth2Test()
        {
            // Test depth 1
            var path = GetExecutionPath("ApplyToEachDepth2Circ");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0, 1),
                new QubitDeclaration(1, 1),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "ApplyToEach",
                    DisplayArgs = "(ApplyDoubleX)",
                    Targets = new List<Register>() {
                        new QubitRegister(0),
                        new QubitRegister(1),
                    },
                    Children = ImmutableList<Operation>.Empty.AddRange(
                        new [] {
                        new Operation()
                        {
                            Gate = "ApplyDoubleX",
                            Targets = new List<Register>() { new QubitRegister(0) },
                            Children = ImmutableList<Operation>.Empty.AddRange(
                                new [] {
                                    new Operation()
                                    {
                                        Gate = "X",
                                        Targets = new List<Register>() { new QubitRegister(0) },
                                    },
                                    new Operation()
                                    {
                                        Gate = "X",
                                        Targets = new List<Register>() { new QubitRegister(0 ) },
                                    },
                                }
                            )
                        },
                        new Operation()
                        {
                            Gate = "ApplyDoubleX",
                            Targets = new List<Register>() { new QubitRegister(1) },
                            Children = ImmutableList<Operation>.Empty.AddRange(
                                new [] {
                                        new Operation()
                                    {
                                        Gate = "X",
                                        Targets = new List<Register>() {    new QubitRegister(1) },
                                    },
                                    new Operation()
                                    {
                                        Gate = "X",
                                        Targets = new List<Register>() { new QubitRegister(1) },
                                    },
                                }
                            )
                        },
                    })
                },
                ResetAll(new int[] { 0, 1 }),
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }
    }

    [TestClass]
    public class MeasurementTests : ExecutionPathTracerTests
    {
        [TestMethod]
        public void MResetXTest()
        {
            var path = GetExecutionPath("MResetXCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0, 1),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "MResetX",
                    IsMeasurement = true,
                    Controls = new List<Register>() { new QubitRegister(0) },
                    Targets = new List<Register>() { new ClassicalRegister(0, 0) },
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }

        [TestMethod]
        public void MResetYTest()
        {
            var path = GetExecutionPath("MResetYCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0, 1),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "MResetY",
                    IsMeasurement = true,
                    Controls = new List<Register>() { new QubitRegister(0) },
                    Targets = new List<Register>() { new ClassicalRegister(0, 0) },
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }

        [TestMethod]
        public void MResetZTest()
        {
            var path = GetExecutionPath("MResetZCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0, 1),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "MResetZ",
                    IsMeasurement = true,
                    Controls = new List<Register>() { new QubitRegister(0) },
                    Targets = new List<Register>() { new ClassicalRegister(0, 0) },
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }

        [TestMethod]
        public void ForEachMeasureCirc()
        {
            var path = GetExecutionPath("ForEachMeasureCirc");
            var qubits = new QubitDeclaration[]
            {
                new QubitDeclaration(0, 1),
                new QubitDeclaration(1, 1),
            };
            var operations = new Operation[]
            {
                new Operation()
                {
                    Gate = "ForEach",
                    DisplayArgs = "(MResetZ)",
                    Targets = new List<Register>() { new QubitRegister(0), new QubitRegister(1) },
                    Children = ImmutableList<Operation>.Empty.AddRange(
                        new [] {
                            new Operation()
                            {
                                Gate = "MResetZ",
                                IsMeasurement = true,
                                Controls = new List<Register>() { new QubitRegister(0) },
                                Targets = new List<Register>() { new ClassicalRegister(0, 0) },
                            },
                            new Operation()
                            {
                                Gate = "MResetZ",
                                IsMeasurement = true,
                                Controls = new List<Register>() { new QubitRegister(1) },
                                Targets = new List<Register>() { new ClassicalRegister(1, 0) },
                            },
                        }
                    ),
                },
            };
            var expected = new ExecutionPath(qubits, operations);
            AssertExecutionPathsEqual(expected, path);
        }
    }
}
