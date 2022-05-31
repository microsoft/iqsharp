// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.Runtime;
using Microsoft.Quantum.Runtime.Submitters;
using Microsoft.Quantum.Simulation.Core;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <inheritdoc/>
    internal class EntryPoint : IEntryPoint
    {
        // The namespace must match the one found in the in CompilerService.cs in the Core project.
        private const string EntryPointNamespaceName = "ENTRYPOINT";

        private object EntryPointInfo { get; }
        private Type InputType { get; }
        private Type OutputType { get; }
        private OperationInfo OperationInfo { get; }
        private ILogger? Logger { get; }

        /// <inheritdoc/>
        public Stream? QirStream { get; }

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
        /// <param name="qirStream">
        ///     Stream from which QIR bitcode for the entry point can be read.
        /// </param>
        /// <param name="logger">Logger used to report internal diagnostics.</param>
        public EntryPoint(object entryPointInfo, Type inputType, Type outputType, OperationInfo operationInfo, Stream? qirStream, ILogger? logger)
        {
            EntryPointInfo = entryPointInfo;
            InputType = inputType;
            OutputType = outputType;
            OperationInfo = operationInfo;
            Logger = logger;
            QirStream = qirStream;
        }

        private object GetEntryPointInputObject(AzureSubmissionContext submissionContext)
        {
            var parameterTypes = new List<Type>();
            var parameterValues = new List<object>();
            foreach (var parameter in OperationInfo.RoslynParameters)
            {
                if (!submissionContext.InputParameters.ContainsKey(parameter.Name))
                {
                    throw new ArgumentException($"Required parameter {parameter.Name} was not specified.");
                }

                var rawParameterValue = submissionContext.InputParameters[parameter.Name];
                try
                {
                    var parameterValue = submissionContext.InputParameters.DecodeParameter(parameter.Name, type: parameter.ParameterType);

                    if (parameterValue != null)
                    {
                        parameterTypes.Add(parameter.ParameterType);
                        parameterValues.Add(parameterValue);
                    }
                }
                catch (Exception e)
                {
                    throw new ArgumentException($"The value {rawParameterValue} provided for parameter {parameter.Name} could not be converted to the expected type: {e.Message}");
                }
            }

            return parameterValues.Count switch
            {
                0 => QVoid.Instance,
                1 => parameterValues.Single(),
                _ => InputType.GetConstructor(parameterTypes.ToArray()).Invoke(parameterValues.ToArray())
            };
        }

        private ArgumentValue GetArgumentValue(string parameterValue, System.Reflection.ParameterInfo parameter)
        {
            var parameterType = parameter.ParameterType;

            if (parameterType == typeof(bool))
            {
                return new ArgumentValue.Bool(Newtonsoft.Json.JsonConvert.DeserializeObject<bool>(parameterValue));
            }
            else if (parameterType == typeof(double))
            {
                return new ArgumentValue.Double(Newtonsoft.Json.JsonConvert.DeserializeObject<double>(parameterValue));
            }
            else if (parameterType == typeof(long))
            {
                return new ArgumentValue.Int(Newtonsoft.Json.JsonConvert.DeserializeObject<long>(parameterValue));
            }
            else if (parameterType == typeof(string))
            {
                return new ArgumentValue.String(parameterValue);
            }
            else if (parameterType == typeof(Pauli))
            {
                return new ArgumentValue.Pauli(Newtonsoft.Json.JsonConvert.DeserializeObject<Pauli>(parameterValue));
            }
            else if (parameterType == typeof(Result))
            {
                return new ArgumentValue.Result(Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(parameterValue)!);
            }
            else
            {
                throw new ArgumentException($"The given type of {parameterType.Name} is not supported."); ;
            }
        }

        private IReadOnlyList<Argument> GetEntryPointInputArguments(AzureSubmissionContext submissionContext)
        {
            var argumentList = new List<Argument>();
            foreach (var parameter in OperationInfo.RoslynParameters)
            {
                if (!submissionContext.InputParameters.ContainsKey(parameter.Name))
                {
                    throw new ArgumentException($"Required parameter {parameter.Name} was not specified.");
                }

                var rawParameterValue = submissionContext.InputParameters[parameter.Name];

                try
                {
                    var argument = new Argument(parameter.Name, GetArgumentValue(rawParameterValue, parameter));
                    argumentList.Add(argument);
                }
                catch (Exception e)
                {
                    throw new ArgumentException($"The value {rawParameterValue} provided for parameter {parameter.Name} could not be converted to the expected type: {e.Message}");
                }
            }

            return argumentList;
        }

        /// <inheritdoc/>
        public Task<IQuantumMachineJob> SubmitAsync(IQuantumMachine machine, AzureSubmissionContext submissionContext, CancellationToken cancellationToken = default)
        {
            var entryPointInput = GetEntryPointInputObject(submissionContext);

            try
            {
                Logger.LogDebug(
                    "About to submit entry point {Name}.",
                    (((dynamic)EntryPointInfo).Operation as Type)?.FullName
                );
            }
            catch {}

            // Find and invoke the method on IQuantumMachine that is declared as:
            // Task<IQuantumMachineJob> SubmitAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input, SubmissionContext context)
            var submitMethod = typeof(IQuantumMachine)
                .GetMethods()
                .Single(method =>
                    method.Name == "SubmitAsync"
                    && method.IsGenericMethodDefinition
                    && method.GetParameters().Length == 3
                    && method.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == EntryPointInfo.GetType().GetGenericTypeDefinition()
                    && method.GetParameters()[1].ParameterType.IsGenericMethodParameter
                    && method.GetParameters()[2].ParameterType == typeof(IQuantumMachineSubmissionContext))
                .MakeGenericMethod(new Type[] { InputType, OutputType });
            var submitParameters = new object[] { EntryPointInfo, entryPointInput, submissionContext };
            return (Task<IQuantumMachineJob>)submitMethod.Invoke(machine, submitParameters);
        }

        /// <inheritdoc/>
        public Task<IQuantumMachineJob> SubmitAsync(IQirSubmitter submitter, AzureSubmissionContext submissionContext, CancellationToken cancellationToken = default)
        {
            if (QirStream is null)
            {
                throw new ArgumentException($"A QIR stream is required when submitting using the IQirSubmitter interface.");
            }

            var entryPointInput = GetEntryPointInputArguments(submissionContext);

            var options = SubmissionOptions.Default.With(submissionContext.FriendlyName, submissionContext.Shots, submissionContext.InputParams);

            // Find and invoke the method on IQirSubmitter that is declared as:
            // Task<IQuantumMachineJob> SubmitAsync(Stream qir, string entryPoint, IReadOnlyList<Argument> arguments, SubmissionOptions submissionOptions)
            var submitMethod = typeof(IQirSubmitter)
                .GetMethods()
                .Single(method =>
                    method.Name == "SubmitAsync"
                    && method.GetParameters().Length == 4
                    && method.GetParameters()[0].ParameterType == typeof(Stream)
                    && method.GetParameters()[1].ParameterType == typeof(string)
                    && method.GetParameters()[2].ParameterType == typeof(IReadOnlyList<Argument>)
                    && method.GetParameters()[3].ParameterType == typeof(SubmissionOptions)
                );
            var submitParameters = new object[] { QirStream, $"{EntryPointNamespaceName}__{submissionContext.OperationName}", entryPointInput, options };
            return (Task<IQuantumMachineJob>)submitMethod.Invoke(submitter, submitParameters);
        }
    }
}
