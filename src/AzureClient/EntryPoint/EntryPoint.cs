﻿// Copyright (c) Microsoft Corporation.
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
        /// <param name="entryPointInfo">Must be an <see cref="EntryPointInfo{I,O}"/> object with type
        /// parameters specified by the types in the <c>entryPointInputbeginWords</c> argument.</param>
        /// <param name="inputType">Specifies the input parameter type for the
        /// <see cref="EntryPointInfo{I,O}"/> object provided as the <c>entryPointInfo</c> argument.</param>
        /// <param name="outputType">Specifies the output parameter type for the
        /// <see cref="EntryPointInfo{I,O}"/> object provided as the <c>entryPointInfo</c> argument.</param>
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
                if (!inputParameters.ContainsKey(parameter.Name))
                {
                    throw new ArgumentException($"Required parameter {parameter.Name} was not specified.");
                }

                string rawParameterValue = inputParameters[parameter.Name];
                object? parameterValue = null;
                try
                {
                    parameterValue = System.Convert.ChangeType(rawParameterValue, parameter.ParameterType);
                }
                catch (Exception e)
                {
                    throw new ArgumentException($"The value {rawParameterValue} provided for parameter {parameter.Name} could not be converted to the expected type: {e.Message}");
                }

                parameterTypes.Add(parameter.ParameterType);
                parameterValues.Add(parameterValue);
            }

            var entryPointInput = parameterValues.Count switch
            {
                0 => QVoid.Instance,
                1 => parameterValues.Single(),
                _ => InputType.GetConstructor(parameterTypes.ToArray()).Invoke(parameterValues.ToArray())
            };

            // Find and invoke the method on IQuantumMachine that is declared as:
            // Task<IQuantumMachineJob> SubmitAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input)
            var submitMethod = typeof(IQuantumMachine)
                .GetMethods()
                .Single(method =>
                    method.Name == "SubmitAsync"
                    && method.IsGenericMethodDefinition
                    && method.GetParameters().Length == 2
                    && method.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == EntryPointInfo.GetType().GetGenericTypeDefinition()
                    && method.GetParameters()[1].ParameterType.IsGenericMethodParameter)
                .MakeGenericMethod(new Type[] { InputType, OutputType });
            var submitParameters = new object[] { EntryPointInfo, entryPointInput };
            return (Task<IQuantumMachineJob>)submitMethod.Invoke(machine, submitParameters);
        }
    }
}
