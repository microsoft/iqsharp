// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Quantum.Runtime;
using Microsoft.Quantum.Simulation.Core;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <inheritdoc/>
    internal class EntryPoint : IEntryPoint
    {
        private readonly object entryPointInfo;
        private readonly Type[] entryPointInputOutputTypes;
        private readonly OperationInfo entryPointOperationInfo;

        /// <summary>
        /// Creates an object used to submit jobs to Azure Quantum.
        /// </summary>
        /// <param name="entryPointInfo">Must be an <see cref="EntryPoint"/> object with type
        /// parameters specified by the types in the <c>entryPointInputOutputTypes</c> argument.</param>
        /// <param name="entryPointInputOutputTypes">Specifies the type parameters for the
        /// <see cref="EntryPoint"/> object provided as the <c>entryPointInfo</c> argument.</param>
        /// <param name="entryPointOperationInfo">Information about the Q# operation to be used as the entry point.</param>
        public EntryPoint(object entryPointInfo, Type[] entryPointInputOutputTypes, OperationInfo entryPointOperationInfo)
        {
            this.entryPointInfo = entryPointInfo;
            this.entryPointInputOutputTypes = entryPointInputOutputTypes;
            this.entryPointOperationInfo = entryPointOperationInfo;
        }

        /// <inheritdoc/>
        public Task<IQuantumMachineJob> SubmitAsync(IQuantumMachine machine, Dictionary<string, string> inputParameters)
        {
            var typedParameters = new List<object>();
            foreach (var parameter in entryPointOperationInfo.RoslynParameters)
            {
                typedParameters.Add(System.Convert.ChangeType(inputParameters[parameter.Name], parameter.ParameterType));
            }

            // TODO: Need to use all of the typed parameters, not just the first one.
            var entryPointInput = typedParameters.DefaultIfEmpty(QVoid.Instance).First();

            var method = typeof(IQuantumMachine).GetMethod("SubmitAsync").MakeGenericMethod(entryPointInputOutputTypes);
            return method.Invoke(machine, new object[] { entryPointInfo, entryPointInput }) as Task<IQuantumMachineJob>;
        }
    }
}
