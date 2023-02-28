// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
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
        /// <param name="logger">Logger used to report internal diagnostics.</param>
        /// <param name="qirStream">
        ///     Stream from which QIR bitcode for the entry point can be read.
        /// </param>
        public EntryPoint(object entryPointInfo, Type inputType, Type outputType, OperationInfo operationInfo, ILogger? logger, Stream? qirStream = null)
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

        private ArgumentValue GetArgumentValue(System.Reflection.ParameterInfo parameter, string parameterValue)
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
            else if (parameterType.IsQArray())
            {
                var arrayType = parameterType.GenericTypeArguments.FirstOrDefault() ??
                    throw new ArgumentException($"Could not get the type of array.");
                if (arrayType == typeof(bool))
                {
                    var values = Newtonsoft.Json.JsonConvert.DeserializeObject<bool[]>(parameterValue)
                        .Select(v => new ArgumentValue.Bool(v))
                        .ToImmutableArray<ArgumentValue>();
                    return ArgumentValue.Array.TryCreate(values, ArgumentType.Bool) ??
                        throw new ArgumentException($"Could not create array of Bool for {parameter.Name}.");
                }
                else if (arrayType == typeof(double))
                {
                    var values = Newtonsoft.Json.JsonConvert.DeserializeObject<double[]>(parameterValue)
                        .Select(v => new ArgumentValue.Double(v))
                        .ToImmutableArray<ArgumentValue>();
                    return ArgumentValue.Array.TryCreate(values, ArgumentType.Double) ??
                        throw new ArgumentException($"Could not create array of Bool for {parameter.Name}.");
                }
                else if (arrayType == typeof(long))
                {
                    var values = Newtonsoft.Json.JsonConvert.DeserializeObject<long[]>(parameterValue)
                        .Select(v => new ArgumentValue.Int(v))
                        .ToImmutableArray<ArgumentValue>();
                    return ArgumentValue.Array.TryCreate(values, ArgumentType.Int) ??
                        throw new ArgumentException($"Could not create array of Int for {parameter.Name}.");
                }
                else if (arrayType == typeof(string)){
                    var values = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(parameterValue)
                        .Select(v => new ArgumentValue.String(v))
                        .ToImmutableArray<ArgumentValue>();
                    return ArgumentValue.Array.TryCreate(values, ArgumentType.String) ??
                        throw new ArgumentException($"Could not create array of String for {parameter.Name}.");
                }
                else if (arrayType == typeof(Pauli))
                {
                    var values = Newtonsoft.Json.JsonConvert.DeserializeObject<Pauli[]>(parameterValue)
                        .Select(v => new ArgumentValue.Pauli(v))
                        .ToImmutableArray<ArgumentValue>();
                    return ArgumentValue.Array.TryCreate(values, ArgumentType.Pauli) ??
                        throw new ArgumentException($"Could not create array of Pauli for {parameter.Name}.");
                }
                else if (arrayType == typeof(Result))
                {
                    var values = Newtonsoft.Json.JsonConvert.DeserializeObject<Result[]>(parameterValue)
                        .Select(v => new ArgumentValue.Result(v))
                        .ToImmutableArray<ArgumentValue>();
                    return ArgumentValue.Array.TryCreate(values, ArgumentType.Result) ??
                        throw new ArgumentException($"Could not create array of Result for {parameter.Name}.");
                }

                throw new ArgumentException($"Unsupported array type {arrayType}.");
            }
            else
            {
                throw new ArgumentException($"The given type of {parameterType.Name} is not supported.");
            }
        }

        private IReadOnlyList<Argument> GetEntryPointInputArguments(AzureSubmissionContext submissionContext, bool allowOptionalParameters)
        {
            var argumentList = new List<Argument>();
            foreach (var parameter in OperationInfo.RoslynParameters)
            {
                if (!submissionContext.InputParameters.ContainsKey(parameter.Name))
                {
                    if (allowOptionalParameters)
                    {
                        continue;
                    }
                    else
                    {
                        throw new ArgumentException($"Required parameter {parameter.Name} was not specified.");
                    }
                }

                var rawParameterValue = submissionContext.InputParameters[parameter.Name];

                try
                {
                    var argument = new Argument(parameter.Name, GetArgumentValue(parameter, rawParameterValue));
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

            // In batching jobs, entry point arguments are specified as part of the `items` arguments (so far only supported in microsoft.estimator)
            var isBatchingJob = submissionContext.InputParams.ContainsKey("items");
            var allowOptionalParameters = isBatchingJob && submitter.Target == AzureClient.MicrosoftEstimator;

            var entryPointInput = GetEntryPointInputArguments(submissionContext, allowOptionalParameters);
            Logger?.LogInformation("Submitting job {FriendlyName} with {NShots} shots.", submissionContext.FriendlyName, submissionContext.Shots);
            var options = SubmissionOptions.Default.With(submissionContext.FriendlyName, submissionContext.Shots, submissionContext.InputParams);

            // Find and invoke the method on IQirSubmitter that is declared as:
            // Task<IQuantumMachineJob> SubmitAsync(Stream qir, string entryPoint, IReadOnlyList<Argument> arguments, SubmissionOptions submissionOptions)
            return submitter.SubmitAsync(QirStream, $"{EntryPointNamespaceName}__{submissionContext.OperationName}", entryPointInput, options);
        }
    }
}
