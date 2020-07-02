// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.Simulation.Core;

#nullable enable

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// Traces through the operations in a given execution path of a Q# program by hooking on
    /// to a simulator via the event listeners <c>OnOperationStartHandler</c> and
    /// <c>OnOperationEndHandler</c>, and generates the corresponding <c>ExecutionPath</c>.
    /// </summary>
    public class ExecutionPathTracer
    {
        private int currDepth = 0;
        private int renderDepth;
        private IDictionary<int, QubitRegister> qubitRegisters = new Dictionary<int, QubitRegister>();
        private IDictionary<int, List<ClassicalRegister>> classicalRegisters = new Dictionary<int, List<ClassicalRegister>>();
        private List<Operation> operations = new List<Operation>();
        private Type[] nestedTypes = new Type[]
        {
            typeof(Microsoft.Quantum.Canon.ApplyToEach<Qubit>),
            typeof(Microsoft.Quantum.Canon.ApplyToEachC<Qubit>),
            typeof(Microsoft.Quantum.Canon.ApplyToEachA<Qubit>),
            typeof(Microsoft.Quantum.Canon.ApplyToEachCA<Qubit>),
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionPathTracer"/> class.
        /// </summary>
        /// <param name="depth">
        /// The depth at which to render operations.
        /// </param>
        public ExecutionPathTracer(int depth = 1) => this.renderDepth = depth + 1;

        /// <summary>
        /// Returns the generated <c>ExecutionPath</c>.
        /// </summary>
        public ExecutionPath GetExecutionPath()
        {
            var qubits = this.qubitRegisters.Keys
                .OrderBy(k => k)
                .Select(k =>
                {
                    var qubitDecl = new QubitDeclaration(k);
                    if (this.classicalRegisters.ContainsKey(k))
                    {
                        qubitDecl.NumChildren = this.classicalRegisters[k].Count;
                    }

                    return qubitDecl;
                })
                .ToArray();

            return new ExecutionPath(qubits, this.operations.ToArray());
        }

        /// <summary>
        /// Provides the event listener to listen to <c>SimulatorBase</c>'s <c>OnOperationStart</c> event.
        /// </summary>
        public void OnOperationStartHandler(ICallable operation, IApplyData arguments)
        {
            // If the operation type is one of the nestedTypes, go one depth deeper and parse
            // those operations instead
            if (this.nestedTypes.Contains(operation.GetType())) return;

            this.currDepth++;

            // Parse operations at specified depth
            if (this.currDepth == this.renderDepth)
            {
                var parsedOp = this.ParseOperation(operation, arguments);
                if (parsedOp != null)
                {
                    this.operations.Add(parsedOp);
                }
            }
        }

        /// <summary>
        /// Provides the event listener to listen to <c>SimulatorBase</c>'s <c>OnOperationEnd</c> event.
        /// </summary>
        public void OnOperationEndHandler(ICallable operation, IApplyData result)
        {
            if (this.nestedTypes.Contains(operation.GetType())) return;
            this.currDepth--;
        }

        private static bool IsPartialApplication(ICallable operation)
        {
            var t = operation.GetType();
            if (t == null) return false;

            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(OperationPartial<,,>);
        }

        /// <summary>
        /// Retrieves the <c>QubitRegister</c> associated with the given <c>Qubit</c> or create a new
        /// one if it doesn't exist.
        /// </summary>
        private QubitRegister GetQubitRegister(Qubit qubit)
        {
            if (!this.qubitRegisters.ContainsKey(qubit.Id))
            {
                this.qubitRegisters[qubit.Id] = new QubitRegister(qubit.Id);
            }

            return this.qubitRegisters[qubit.Id];
        }

        private List<QubitRegister> GetQubitRegisters(IEnumerable<Qubit> qubits)
        {
            return qubits.Select(this.GetQubitRegister).ToList();
        }

        /// <summary>
        /// Creates a new <c>ClassicalRegister</c> and associate it with the given <c>Qubit</c>.
        /// </summary>
        private ClassicalRegister CreateClassicalRegister(Qubit measureQubit)
        {
            var qId = measureQubit.Id;

            if (!this.classicalRegisters.ContainsKey(qId))
            {
                this.classicalRegisters[qId] = new List<ClassicalRegister>();
            }

            var cId = this.classicalRegisters[qId].Count;
            ClassicalRegister register = new ClassicalRegister(qId, cId);

            // Add classical register under the given qubit id
            this.classicalRegisters[qId].Add(register);

            return register;
        }

        /// <summary>
        /// Retrieves the most recent <c>ClassicalRegister</c> associated with the given <c>Qubit</c>.
        /// </summary>
        /// <remarks>
        /// Currently not used as this is intended for classically-controlled operations.
        /// </remarks>
        private ClassicalRegister GetClassicalRegister(Qubit controlQubit)
        {
            var qId = controlQubit.Id;
            if (!this.classicalRegisters.ContainsKey(qId) || this.classicalRegisters[qId].Count == 0)
            {
                throw new Exception("No classical registers found for qubit {qId}.");
            }

            // Get most recent measurement on given control qubit
            var cId = this.classicalRegisters[qId].Count - 1;
            return this.classicalRegisters[qId][cId];
        }

        /// <summary>
        /// Given a <c>Type</c> and its value, extract its fields and format it as a string.
        /// </summary>
        private string? ExtractArgs(Type t, object value)
        {
            List<string?> fields = new List<string?>();

            foreach (var f in t.GetFields())
            {
                // If field is a tuple, recursively extract its inner arguments and format as a tuple string.
                if (f.FieldType.IsTuple())
                {
                    var nestedArgs = f.GetValue(value);
                    if (nestedArgs != null)
                    {
                        var nestedFields = this.ExtractArgs(f.FieldType, nestedArgs);
                        fields.Add(nestedFields);
                    }
                }
                // Add field as an argument if it is not a Qubit type
                else if (!f.FieldType.IsQubitsContainer())
                {
                    fields.Add(f.GetValue(value)?.ToString());
                }
            }

            return fields.Any() ? $"({string.Join(",", fields.WhereNotNull())})" : null;
        }

        /// <summary>
        /// Given an operation and its arguments, parse it into an <c>Operation</c> object.
        /// </summary>
        private Operation? ParseOperation(ICallable operation, IApplyData arguments)
        {
            // If operation is a partial application, perform on baseOp recursively.
            if (IsPartialApplication(operation))
            {
                dynamic partialOp = operation;
                dynamic partialOpArgs = arguments;

                // Recursively get base operation operations
                var baseOp = partialOp.BaseOp;
                var baseArgs = baseOp.__dataIn(partialOpArgs.Value);
                return this.ParseOperation(baseOp, baseArgs);
            }

            var controlled = operation.Variant == OperationFunctor.Controlled ||
                             operation.Variant == OperationFunctor.ControlledAdjoint;
            var adjoint = operation.Variant == OperationFunctor.Adjoint ||
                          operation.Variant == OperationFunctor.ControlledAdjoint;

            // If operation is controlled, perform on baseOp recursively and mark as controlled.
            if (controlled)
            {
                dynamic ctrlOp = operation;
                dynamic ctrlOpArgs = arguments;

                var ctrls = ctrlOpArgs.Value.Item1;
                var controlRegs = this.GetQubitRegisters(ctrls);

                // Recursively get base operation operations
                var baseOp = ctrlOp.BaseOp;
                var baseArgs = baseOp.__dataIn(ctrlOpArgs.Value.Item2);
                var parsedBaseOp = this.ParseOperation(baseOp, baseArgs);

                parsedBaseOp.Controlled = true;
                parsedBaseOp.Adjoint = adjoint;
                parsedBaseOp.Controls.InsertRange(0, controlRegs);

                return parsedBaseOp;
            }

            // Handle operation based on type
            switch (operation)
            {
                // Handle CNOT operations as a Controlled X
                case Microsoft.Quantum.Intrinsic.CNOT cnot:
                case Microsoft.Quantum.Intrinsic.CCNOT ccnot:
                    var ctrlRegs = new List<Register>();
                    var targetRegs = new List<Register>();

                    switch (arguments.Value)
                    {
                        case ValueTuple<Qubit, Qubit> cnotQs:
                            var (ctrl, cnotTarget) = cnotQs;
                            ctrlRegs.Add(this.GetQubitRegister(ctrl));
                            targetRegs.Add(this.GetQubitRegister(cnotTarget));
                            break;
                        case ValueTuple<Qubit, Qubit, Qubit> ccnotQs:
                            var (ctrl1, ctrl2, ccnotTarget) = ccnotQs;
                            ctrlRegs.Add(this.GetQubitRegister(ctrl1));
                            ctrlRegs.Add(this.GetQubitRegister(ctrl2));
                            targetRegs.Add(this.GetQubitRegister(ccnotTarget));
                            break;
                    }

                    return new Operation
                    {
                        Gate = "X",
                        Controlled = true,
                        Adjoint = adjoint,
                        Controls = ctrlRegs,
                        Targets = targetRegs,
                    };

                // Measurement operations
                case Microsoft.Quantum.Intrinsic.M m:
                case Microsoft.Quantum.Measurement.MResetX mx:
                case Microsoft.Quantum.Measurement.MResetY my:
                case Microsoft.Quantum.Measurement.MResetZ mz:
                    var measureQubit = arguments.GetQubits().ElementAt(0);
                    var measureReg = this.GetQubitRegister(measureQubit);
                    var clsReg = this.CreateClassicalRegister(measureQubit);

                    return new Operation
                    {
                        Gate = "measure",
                        Controlled = false,
                        Adjoint = adjoint,
                        Controls = new List<Register>() { measureReg },
                        Targets = new List<Register>() { clsReg },
                    };

                // Operations to ignore
                case Microsoft.Quantum.Intrinsic.Reset reset:
                case Microsoft.Quantum.Intrinsic.ResetAll resetAll:
                    return null;

                // General operations
                default:
                    Type t = arguments.Value.GetType();
                    var argStr = this.ExtractArgs(t, arguments.Value);
                    var qubitRegs = this.GetQubitRegisters(arguments.GetQubits());

                    return new Operation
                    {
                        Gate = operation.Name,
                        ArgStr = argStr,
                        Controlled = false,
                        Adjoint = adjoint,
                        Controls = new List<Register>(),
                        Targets = qubitRegs.Cast<Register>().ToList(),
                    };
            }
        }
    }
}