// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.Quantum.IQSharp.Core.ExecutionPathTracer
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
        /// A list of <see cref="QubitDeclaration"/> that represents the declared qubits used in the execution path.
        /// </param>
        /// <param name="operations">
        /// A list of <see cref="Operation"/> that represents the operations used in the execution path.
        /// </param>
        public ExecutionPath(IEnumerable<QubitDeclaration> qubits, IEnumerable<Operation> operations)
        {
            this.Qubits = qubits;
            this.Operations = operations;
        }

        /// <summary>
        /// A list of <see cref="QubitDeclaration"/> that represents the declared qubits used in the execution path.
        /// </summary>
        public IEnumerable<QubitDeclaration> Qubits { get; private set; }

        /// <summary>
        /// A list of <see cref="Operation"/> that represents the operations used in the execution path.
        /// </summary>
        public IEnumerable<Operation> Operations { get; private set; }

        /// <summary>
        /// Serializes <see cref="ExecutionPath"/> into its JSON representation.
        /// </summary>
        /// <param name="prettyPrint">
        /// Pretty prints the JSON (i.e. with white space and indents) if <c>true</c>.
        /// </param>
        public string ToJson(bool prettyPrint = false) =>
            JsonSerializer.Serialize(this,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    IgnoreNullValues = true,
                    WriteIndented = prettyPrint,
                }
            );
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
        public int Id { get; private set; }

        /// <summary>
        /// Number of associated classical registers.
        /// </summary>
        public int? NumChildren { get; private set; }
    }

    /// <summary>
    /// Represents an operation used in an execution path.
    /// </summary>
    public class Operation
    {
        /// <summary>
        /// Label of gate.
        /// </summary>
        public string Gate { get; set; } = "";

        /// <summary>
        /// Non-qubit arguments provided to gate.
        /// </summary>
        public string? ArgStr { get; set; }

        /// <summary>
        /// Group of operations for each classical branch.
        /// </summary>
        /// <remarks>
        /// Currently not used as this is intended for classically-controlled operations.
        /// </remarks>
        public IEnumerable<IEnumerable<Operation>> Children { get; set; } = new List<List<Operation>>();

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
        public IEnumerable<Register> Controls { get; set; } = new List<Register>();

        /// <summary>
        /// List of target registers.
        /// </summary>
        public IEnumerable<Register> Targets { get; set; } = new List<Register>();
    }
}
