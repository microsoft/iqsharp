﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.Runtime;
using System.Collections.Immutable;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    /// Represents the configuration settings for a job submission to Azure Quantum.
    /// </summary>
    public sealed class AzureSubmissionContext : IQuantumMachineSubmissionContext
    {
        private static readonly ImmutableDictionary<string, string> DefaultJobParams = ImmutableDictionary<string, string>.Empty;
        private static readonly int DefaultShots = 500;
        private static readonly int DefaultExecutionTimeoutInSeconds = 30;
        private static readonly int DefaultExecutionPollingIntervalInSeconds = 5;
        
        internal static readonly string ParameterNameOperationName = "__operationName__";
        internal static readonly string ParameterNameJobName = "jobName";
        internal static readonly string ParameterNameJobParams = "jobParams";
        internal static readonly string ParameterNameShots = "shots";
        internal static readonly string ParameterNameTimeout = "timeout";
        internal static readonly string ParameterNamePollingInterval = "poll";

        /// <inheritdoc/>
        public string FriendlyName { get; set; } = string.Empty;

        /// <inheritdoc/>
        public int Shots { get; set; } = DefaultShots;

        /// <summary>
        ///     The Q# operation name to be executed as part of this job.
        /// </summary>
        public string OperationName { get; set; } = string.Empty;

        /// <summary>
        ///     Provider-specific job parameters to be passed to the execution target, expressed as one or more JSON {"key":"value",...} pairs.
        /// </summary>
        /// <remarks>
        ///     These parameters apply to <c>%azure.execute</c> and <c>%azure.submit</c>. The JSON may not contain separating spaces when used in a Jupyter notebook. 
        /// </remarks>
        //
        // NOTE: This property was named "InputParams" (instead of "JobParams") because the closely
        //       related implementation in microsoft/qsharp-runtime used the name "InputParams"
        //       (see https://github.com/microsoft/qsharp-runtime/pull/829).
        //
        //       Please notice the difference between "InputParams" and the preexisting "InputParameters"
        //       member below.
        public ImmutableDictionary<string, string> InputParams { get; set; } = ImmutableDictionary<string, string>.Empty;

        /// <summary>
        ///     The input parameters to be provided to the specified Q# operation.
        /// </summary>
        public Dictionary<string, string> InputParameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        ///     The execution timeout for the job, expressed in seconds.
        /// </summary>
        /// <remarks>
        ///     This setting only applies to <c>%azure.execute</c>. It is ignored for <c>%azure.submit</c>.
        ///     The timeout determines how long the IQ# kernel will wait for the job to complete;
        ///     the Azure Quantum job itself will continue to execute until it is completed.
        /// </remarks>
        public int ExecutionTimeout { get; set; } = DefaultExecutionTimeoutInSeconds;

        /// <summary>
        ///     The polling interval, in seconds, to check for job status updates
        ///     while waiting for an Azure Quantum job to complete execution.
        /// </summary>
        /// <remarks>
        ///     This setting only applies to <c>%azure.execute</c>. It is ignored for <c>%azure.submit</c>.
        /// </remarks>
        public int ExecutionPollingInterval { get; set; } = DefaultExecutionPollingIntervalInSeconds;

        /// <summary>
        ///     Parses the input from a magic command into an <see cref="AzureSubmissionContext"/> object
        ///     suitable for job submission via <see cref="IAzureClient"/>.
        /// </summary>
        public static AzureSubmissionContext Parse(string inputCommand)
        {
            var inputParameters = AbstractMagic.ParseInputParameters(inputCommand, firstParameterInferredName: ParameterNameOperationName);
            var operationName = inputParameters.DecodeParameter<string>(ParameterNameOperationName);
            var jobName = inputParameters.DecodeParameter<string>(ParameterNameJobName, defaultValue: operationName);
            var jobParams = inputParameters.DecodeParameter<ImmutableDictionary<string, string>>(ParameterNameJobParams, defaultValue: DefaultJobParams);
            var shots = inputParameters.DecodeParameter<int>(ParameterNameShots, defaultValue: DefaultShots);
            var timeout = inputParameters.DecodeParameter<int>(ParameterNameTimeout, defaultValue: DefaultExecutionTimeoutInSeconds);
            var pollingInterval = inputParameters.DecodeParameter<int>(ParameterNamePollingInterval, defaultValue: DefaultExecutionPollingIntervalInSeconds);

            return new AzureSubmissionContext()
            {
                FriendlyName = jobName,
                Shots = shots,
                OperationName = operationName,
                InputParams = jobParams,
                InputParameters = inputParameters,
                ExecutionTimeout = timeout,
                ExecutionPollingInterval = pollingInterval,
            };
        }
    }
}
