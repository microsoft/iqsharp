// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
        private Operation? currentOperation = null;
        private IDictionary<int, QubitRegister> qubitRegisters = new Dictionary<int, QubitRegister>();
        private IDictionary<int, List<ClassicalRegister>> classicalRegisters = new Dictionary<int, List<ClassicalRegister>>();
        private List<Operation> operations = new List<Operation>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionPathTracer"/> class with the depth to render operations at.
        /// </summary>
        /// <param name="depth">
        /// The depth at which to render operations.
        /// </param>
        public ExecutionPathTracer(int depth = 1) => this.renderDepth = depth + 1;

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
                this.operations
            );

        /// <summary>
        /// Provides the event listener to listen to
        /// <see cref="Microsoft.Quantum.Simulation.Common.SimulatorBase"/>'s
        /// <c>OnOperationStart</c> event.
        /// </summary>
        public void OnOperationStartHandler(ICallable operation, IApplyData arguments)
        {
            this.currentDepth++;

            // Parse operations at or above specified depth
            if (this.currentDepth <= this.renderDepth)
            {
                var metadata = operation.GetRuntimeMetadata(arguments);

                // Save parsed operation as a potential candidate for rendering.
                // We only want to render the operation at the lowest depth, so we keep
                // a running track of the lowest operation seen in the stack thus far.
                this.currentOperation = this.MetadataToOperation(metadata);
            }
        }

        /// <summary>
        /// Provides the event listener to listen to
        /// <see cref="Microsoft.Quantum.Simulation.Common.SimulatorBase"/>'s
        /// <c>OnOperationEnd</c> event.
        /// </summary>
        public void OnOperationEndHandler(ICallable operation, IApplyData result)
        {
            this.currentDepth--;
            // Add parsed operation to list of operations, if not null
            if (this.currentOperation != null)
            {
                this.operations.Add(this.currentOperation);
                // Reset current operation to null
                this.currentOperation = null;
            }
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
                Children = metadata.Children?.Select(child => child.Select(this.MetadataToOperation).WhereNotNull()),
                Controlled = metadata.IsControlled,
                Adjoint = metadata.IsAdjoint,
                Controls = this.GetQubitRegisters(metadata.Controls),
                Targets = this.GetQubitRegisters(metadata.Targets),
            };

            // Create classical registers for measurement operations
            if (metadata.IsMeasurement)
            {
                var measureQubit = metadata.Targets.ElementAt(0);
                var clsReg = this.CreateClassicalRegister(measureQubit);
                // TODO: Change this to using IsMeasurement
                op.Gate = "measure";
                op.Controls = op.Targets;
                op.Targets = new List<Register>() { clsReg };
            }

            return op;
        }
    }
}
