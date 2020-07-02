// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// Represents the qubit resources and operations traced out in an execution path of a Q# operation.
    /// </summary>
    public class ExecutionPath
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionPathTracer"/> class.
        /// </summary>
        /// <param name="qubits">
        /// An array of <c>QubitDeclaration</c>s that represents the declared qubits used in the execution path.
        /// </param>
        /// <param name="operations">
        /// An array of <c>Operation</c>s that represents the operations used in the execution path.
        /// </param>
        public ExecutionPath(QubitDeclaration[] qubits, Operation[] operations)
        {
            this.Qubits = qubits;
            this.Operations = operations;
        }

        /// <summary>
        /// An array of <c>QubitDeclaration</c>s that represents the declared qubits used in the execution path.
        /// </summary>
        public QubitDeclaration[] Qubits { get; set; }

        /// <summary>
        /// An array of <c>Operation</c>s that represents the operations used in the execution path.
        /// </summary>
        public Operation[] Operations { get; set; }

        /// <summary>
        /// Serializes <c>ExecutionPath</c> into its JSON representation.
        /// </summary>
        /// <param name="prettyPrint">
        /// Pretty prints the JSON (i.e. with white space and indents) if true (default = false).
        /// </param>
        public string ToJson(bool prettyPrint = false)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true,
                WriteIndented = prettyPrint,
            };
            return JsonSerializer.Serialize(this, options);
        }
    }

    /// <summary>
    /// Represents a qubit resource used in an execution path.
    /// </summary>
    public class QubitDeclaration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QubitDeclaration"/> class.
        /// </summary>
        /// <param name="id">
        /// Id of qubit.
        /// </param>
        /// <param name="numChildren">
        /// Number of associated classical registers.
        /// </param>
        public QubitDeclaration(int id, int? numChildren = null)
        {
            this.Id = id;
            this.NumChildren = numChildren;
        }

        /// <summary>
        /// Id of qubit.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Number of associated classical registers.
        /// </summary>
        public int? NumChildren { get; set; }
    }

    /// <summary>
    /// Represents an operation used in an execution path.
    /// </summary>
    public class Operation
    {
        /// <summary>
        /// Label of gate.
        /// </summary>
        public string Gate { get; set; }

        /// <summary>
        /// Non-qubit arguments provided to gate.
        /// </summary>
        public string ArgStr { get; set; }

        /// <summary>
        /// Group of operations for each classical branch.
        /// </summary>
        /// <remarks>
        /// Currently not used as this is intended for classically-controlled operations.
        /// </remarks>
        public List<List<Operation>> Children { get; set; }

        /// <summary>
        /// True if operation is a controlled operations.
        /// </summary>
        public bool Controlled { get; set; }

        /// <summary>
        /// True if operation is an adjoint operations.
        /// </summary>
        public bool Adjoint { get; set; }

        /// <summary>
        /// List of control registers.
        /// </summary>
        public List<Register> Controls { get; set; } = new List<Register>();

        /// <summary>
        /// List of target registers.
        /// </summary>
        public List<Register> Targets { get; set; } = new List<Register>();
    }
}
