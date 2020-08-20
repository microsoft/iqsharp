// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.Simulation.Core;

#nullable enable

namespace Microsoft.Quantum.IQSharp.ExecutionPathTracer
{
    /// <summary>
    /// Traces through the operations in a given execution path of a Q# program by hooking on
    /// to a simulator via the event listeners <see cref="OnOperationStartHandler"/> and
    /// <see cref="OnOperationEndHandler"/>, and generates the corresponding <see cref="ExecutionPath"/>.
    /// </summary>
    public class ExecutionPathTracer
    {
        private IDictionary<int, QubitRegister> qubitRegisters = new Dictionary<int, QubitRegister>();
        private IDictionary<int, List<ClassicalRegister>> classicalRegisters = new Dictionary<int, List<ClassicalRegister>>();

        /// <summary>
        /// Current stack of processed <see cref="Operation"/>s.
        /// </summary>
        public Stack<Operation?> operations = new Stack<Operation?>();

        /// <summary>
        /// Returns the generated <see cref="ExecutionPath"/>.
        /// </summary>
        public ExecutionPath GetExecutionPath() =>
            new ExecutionPath(
                this.qubitRegisters.Keys
                    .OrderBy(k => k)
                    .Select(k => new QubitDeclaration(k, (this.classicalRegisters.ContainsKey(k))
                        ? this.classicalRegisters[k].Count
                        : 0
                    )),
                this.operations.ToList().WhereNotNull()
            );

        /// <summary>
        /// Provides the event listener to listen to
        /// <see cref="Microsoft.Quantum.Simulation.Common.SimulatorBase"/>'s
        /// <c>OnOperationStart</c> event.
        /// </summary>
        public void OnOperationStartHandler(ICallable operation, IApplyData arguments)
        {
            // We don't want to process operations whose parent is a measurement gate (will mess up gate visualization)
            var metadata = (this.operations.Count == 0) || (!this.operations.Peek()?.IsMeasurement ?? true)
                ? operation.GetRuntimeMetadata(arguments)
                : null;

            // We also push on `null` operations to the stack instead of ignoring them so that we pop off the
            // correct element in `OnOperationEndHandler`.
            this.operations.Push(this.MetadataToOperation(metadata));
        }

        /// <summary>
        /// Provides the event listener to listen to
        /// <see cref="Microsoft.Quantum.Simulation.Common.SimulatorBase"/>'s
        /// <c>OnOperationEnd</c> event.
        /// </summary>
        public void OnOperationEndHandler(ICallable operation, IApplyData result)
        {
            if (this.operations.Count <= 1) return;
            if (!this.operations.TryPop(out var currentOperation) || currentOperation == null) return;
            if (!this.operations.TryPeek(out var parentOp) || parentOp == null) return;

            // CNOTs are Controlled X under the hood, so we don't need to render the nested CNOT
            if ((currentOperation.Gate == "X" && currentOperation.IsControlled) &&
                (parentOp.Gate == "X" && parentOp.IsControlled)) return;

            // Add operation to parent operation's children
            parentOp.Children = (parentOp.Children ?? ImmutableList<Operation>.Empty).Add(currentOperation);

            // If parent op is a conditional statement, the first child is rendered onZero and the second onOne
            if (parentOp.IsConditional)
            {
                if (parentOp.Children.Count() == 1) currentOperation.ConditionalRender = ConditionalRender.OnZero;
                else currentOperation.ConditionalRender = ConditionalRender.OnOne;
            } else if (parentOp.ConditionalRender != null)
            {
                // Inherit parent's render condition
                currentOperation.ConditionalRender = parentOp.ConditionalRender;
            }

            // Add target qubits to parent
            parentOp.Targets = parentOp.Targets
                .Concat(currentOperation.Targets.Where(reg => reg is QubitRegister))
                .Distinct();
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
        /// Parse <see cref="RuntimeMetadata"/> into its corresponding <see cref="Operation"/>.
        /// </summary>
        private Operation? MetadataToOperation(RuntimeMetadata? metadata)
        {
            if (metadata == null) return null;

            var displayArgs = (metadata.FormattedNonQubitArgs.Length > 0)
                ? metadata.FormattedNonQubitArgs
                : null;

            // Add surrounding parentheses around displayArgs if it doesn't already have it (i.e. not a tuple)
            if (displayArgs != null && !displayArgs.StartsWith("(")) displayArgs = $"({displayArgs})";

            var op = new Operation()
            {
                Gate = metadata.Label,
                DisplayArgs = displayArgs,
                IsConditional = metadata.IsConditional,
                IsControlled = metadata.IsControlled,
                IsAdjoint = metadata.IsAdjoint,
                Controls = this.GetQubitRegisters(metadata.Controls),
                Targets = this.GetQubitRegisters(metadata.Targets),
            };

            // Create classical registers for measurement operations
            if (metadata.IsMeasurement)
            {
                var measureQubit = metadata.Targets.ElementAt(0);
                var clsReg = this.CreateClassicalRegister(measureQubit);
                op.IsMeasurement = true;
                op.Controls = op.Targets;
                op.Targets = new List<Register>() { clsReg };
            }

            return op;
        }
    }
}
