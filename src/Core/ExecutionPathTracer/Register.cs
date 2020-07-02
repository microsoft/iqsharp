// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// Enum for the 2 types of registers: Qubit and Classical.
    /// </summary>
    public enum RegisterType
    {
        Qubit,
        Classical,
    }

    /// <summary>
    /// Represents a register used by an <c>Operation</c>.
    /// </summary>
    public class Register
    {
        /// <summary>
        /// Type of register.
        /// </summary>
        public virtual RegisterType Type { get; set; }

        /// <summary>
        /// Qubit id of register.
        /// </summary>
        public virtual int QId { get; set; }

        /// <summary>
        /// Classical bit id of register.
        /// </summary>
        public virtual int? CId { get; set; }
    }

    /// <summary>
    /// Represents a qubit register used by an <c>Operation</c>.
    /// </summary>
    public class QubitRegister : Register
    {
        /// <summary>
        /// Creates a new <c>QubitRegister</c> with the given qubit id.
        /// </summary>
        /// <param name="qId">
        /// Id of qubit register.
        /// </param>
        public QubitRegister(int qId)
        {
            this.QId = qId;
        }

        public override RegisterType Type => RegisterType.Qubit;
    }

    /// <summary>
    /// Represents a classical register used by an <c>Operation</c>.
    /// </summary>
    public class ClassicalRegister : Register
    {
        /// <summary>
        /// Creates a new <c>ClassicalRegister</c> with the given qubit id and classical bit id.
        /// </summary>
        /// <param name="qId">
        /// Id of qubit register.
        /// </param>
        /// <param name="cId">
        /// Id of classical register associated with the given qubit id.
        /// </param>
        public ClassicalRegister(int qId, int cId)
        {
            this.QId = qId;
            this.CId = cId;
        }

        public override RegisterType Type => RegisterType.Classical;
    }
}