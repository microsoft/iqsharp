// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.Simulation.Core;

#nullable enable

namespace Microsoft.Quantum.IQSharp.Core.ExecutionPathTracer
{
    /// <summary>
    /// Traces through the operations in a given execution path of a Q# program by hooking on
    /// to a simulator via the event listeners <see cref="OnOperationStartHandler"/> and
    /// <see cref="OnOperationEndHandler"/>, and generates the corresponding <see cref="ExecutionPath"/>.
    /// </summary>
    public class ExecutionPathTracer
    {
        private int currentDepth = 0;
        private int renderDepth;
        private IDictionary<int, QubitRegister> qubitRegisters = new Dictionary<int, QubitRegister>();
        private IDictionary<int, List<ClassicalRegister>> classicalRegisters = new Dictionary<int, List<ClassicalRegister>>();
        private List<Operation> operations = new List<Operation>();
        private readonly ImmutableList<Type> nestedTypes =
            ImmutableList.Create(
                typeof(Microsoft.Quantum.Canon.ApplyToEach<Qubit>),
                typeof(Microsoft.Quantum.Canon.ApplyToEachC<Qubit>),
                typeof(Microsoft.Quantum.Canon.ApplyToEachA<Qubit>),
                typeof(Microsoft.Quantum.Canon.ApplyToEachCA<Qubit>)
            );

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionPathTracer"/> class.
        /// </summary>
        /// <param name="depth">
        /// The depth at which to render operations.
        /// </param>
        public ExecutionPathTracer(int depth = 1)
            => this.renderDepth = depth + 1;

        /// <summary>
        /// Returns the generated <c>ExecutionPath</c>.
        /// </summary>
        public ExecutionPath GetExecutionPath() =>
            new ExecutionPath(
                this.qubitRegisters.Keys
                    .OrderBy(k => k)
                    .Select(k => new QubitDeclaration(k,
                        // Get number of classical registers associated with qubit register (null if none).
                        (this.classicalRegisters.ContainsKey(k))
                            ? this.classicalRegisters[k].Count
                            : null as int?
                    )),
                this.operations
            );

        /// <summary>
        /// Provides the event listener to listen to
        /// <see cref="Microsoft.Quantum.Simulation.Common.SimulatorBase"/>'s
        /// <c>OnOperationStart</c> event.
        /// </summary>
        public void OnOperationStartHandler(ICallable operation, IApplyData arguments)
        {
            // If the operation type is one of the nestedTypes, go one depth deeper and parse
            // those operations instead
            if (this.nestedTypes.Contains(operation.GetType())) return;

            this.currentDepth++;

            // Parse operations at specified depth
            if (this.currentDepth == this.renderDepth)
            {
                var parsedOp = this.ParseOperation(operation, arguments);
                if (parsedOp != null) this.operations.Add(parsedOp);
            }
        }

        /// <summary>
        /// Provides the event listener to listen to
        /// <see cref="Microsoft.Quantum.Simulation.Common.SimulatorBase"/>'s
        /// <c>OnOperationEnd</c> event.
        /// </summary>
        public void OnOperationEndHandler(ICallable operation, IApplyData result)
        {
            if (this.nestedTypes.Contains(operation.GetType())) return;
            this.currentDepth--;
        }

        private static bool IsPartialApplication(ICallable operation)
        {
            var t = operation.GetType();
            if (t == null) return false;

            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(OperationPartial<,,>);
        }

        /// <summary>
        /// Retrieves the <see cref="QubitRegister"/> associated with the given <see cref="Qubit"/> or create a new
        /// one if it doesn't exist.
        /// </summary>
        private QubitRegister GetQubitRegister(Qubit qubit) =>
            this.qubitRegisters.GetOrCreate(qubit.Id, new QubitRegister(qubit.Id));

        private List<QubitRegister> GetQubitRegisters(IEnumerable<Qubit> qubits) =>
            qubits.Select(this.GetQubitRegister).ToList();

        /// <summary>
        /// Creates a new <see cref="ClassicalRegister"/> and associate it with the given <see cref="Qubit"/>.
        /// </summary>
        private ClassicalRegister CreateClassicalRegister(Qubit measureQubit)
        {
            var qId = measureQubit.Id;
            var cId = this.classicalRegisters.GetOrCreate(qId).Count;

            var register = new ClassicalRegister(qId, cId);

            // Add classical register under the given qubit id
            this.classicalRegisters[qId].Add(register);

            return register;
        }

        /// <summary>
        /// Retrieves the most recent <see cref="ClassicalRegister"/> associated with the given <see cref="Qubit"/>.
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
        /// Given an operation and its arguments, parse it into an <see cref="Operation"/> object.
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
                    var argStr = arguments.Value.GetType().ArgsToString(arguments.Value);
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