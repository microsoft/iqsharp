﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
        private object EntryPointInfo { get; }
        private Type InputType { get; }
        private Type OutputType { get; }
        private OperationInfo OperationInfo { get; }

        /// <summary>
        /// Creates an object used to submit jobs to Azure Quantum.
        /// </summary>
        /// <param name="entryPointInfo">Must be an <see cref="Simulation.Core.EntryPointInfo"/> object with type
        /// parameters specified by the types in the <c>entryPointInputbeginWords</c> argument.</param>
        /// <param name="inputType">Specifies the input parameter type for the
        /// <see cref="Simulation.Core.EntryPointInfo"/> object provided as the <c>entryPointInfo</c> argument.</param>
        /// <param name="outputType">Specifies the output parameter type for the
        /// <see cref="Simulation.Core.EntryPointInfo"/> object provided as the <c>entryPointInfo</c> argument.</param>
        /// <param name="operationInfo">Information about the Q# operation to be used as the entry point.</param>
        public EntryPoint(object entryPointInfo, Type inputType, Type outputType, OperationInfo operationInfo)
        {
            EntryPointInfo = entryPointInfo;
            InputType = inputType;
            OutputType = outputType;
            OperationInfo = operationInfo;
        }

        /// <inheritdoc/>
        public Task<IQuantumMachineJob> SubmitAsync(IQuantumMachine machine, Dictionary<string, string> inputParameters)
        {
            var parameterTypes = new List<Type>();
            var parameterValues = new List<object>();
            foreach (var parameter in OperationInfo.RoslynParameters)
            {
                parameterTypes.Add(parameter.ParameterType);
                parameterValues.Add(System.Convert.ChangeType(inputParameters[parameter.Name], parameter.ParameterType));
            }

            var entryPointInput =
                parameterValues.Count == 0 ? QVoid.Instance :
                parameterValues.Count == 1 ? parameterValues.Single() :
                InputType.GetConstructor(parameterTypes.ToArray()).Invoke(parameterValues.ToArray());

            var submitMethod = typeof(IQuantumMachine).GetMethod("SubmitAsync").MakeGenericMethod(new Type[] { InputType, OutputType });
            var submitParameters = new object[] { EntryPointInfo, entryPointInput };
            return submitMethod.Invoke(machine, submitParameters) as Task<IQuantumMachineJob>;
        }
    }
}
