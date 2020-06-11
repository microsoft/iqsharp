// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.Runtime;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    /// Represents the configuration settings for a job submission to Azure Quantum.
    /// </summary>
    public sealed class AzureSubmissionContext : IQuantumMachineSubmissionContext
    {
        private static readonly int DefaultShots = 500;
        private static readonly int DefaultExecutionTimeoutInSeconds = 30;
        private static readonly int DefaultExecutionPollingIntervalInSeconds = 5;

        /// <inheritdoc/>
        public string FriendlyName { get; set; } = string.Empty;

        /// <inheritdoc/>
        public int Shots { get; set; } = DefaultShots;

        /// <summary>
        ///     The Q# operation name to be executed as part of this job.
        /// </summary>
        public string OperationName { get; set; } = string.Empty;

        /// <summary>
        ///     The input parameters to be provided to the specified Q# operation.
        /// </summary>
        public Dictionary<string, string> InputParameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        ///     The execution timeout for the job, expressed in seconds.
        /// </summary>
        /// <remarks>
        ///     This setting only applies to %azure.execute. It is ignored for %azure.submit.
        ///     The timeout determines how long the IQ# kernel will wait for the job to complete;
        ///     the Azure Quantum job itself will continue to execute until it is completed.
        /// </remarks>
        public int ExecutionTimeout { get; set; } = DefaultExecutionTimeoutInSeconds;

        /// <summary>
        ///     The polling interval, in seconds, to check for job status updates
        ///     while waiting for an Azure Quantum job to complete execution.
        /// </summary>
        /// <remarks>
        ///     This setting only applies to %azure.execute. It is ignored for %azure.submit.
        /// </remarks>
        public int ExecutionPollingInterval { get; set; } = DefaultExecutionPollingIntervalInSeconds;

        /// <summary>
        ///     Parses the input from a magic command into an <see cref="AzureSubmissionContext"/> object
        ///     suitable for job submission via <see cref="IAzureClient"/>.
        /// </summary>
        public static AzureSubmissionContext Parse(string inputCommand)
        {
            var parameterNameOperationName = "operationName";
            var parameterNameJobName = "jobName";
            var parameterNameShots = "shots";
            var parameterNameTimeout = "timeout";
            var parameterNamePollingInterval = "pollingInterval";

            var inputParameters = AbstractMagic.ParseInputParameters(inputCommand, firstParameterInferredName: parameterNameOperationName);
            var operationName = inputParameters.DecodeParameter<string>(parameterNameOperationName);
            var jobName = inputParameters.DecodeParameter<string>(parameterNameJobName, defaultValue: operationName);
            var shots = inputParameters.DecodeParameter<int>(parameterNameShots, defaultValue: DefaultShots);
            var timeout = inputParameters.DecodeParameter<int>(parameterNameTimeout, defaultValue: DefaultExecutionTimeoutInSeconds);
            var pollingInterval = inputParameters.DecodeParameter<int>(parameterNamePollingInterval, defaultValue: DefaultExecutionPollingIntervalInSeconds);

            var decodedParameters = inputParameters.ToDictionary(
                item => item.Key,
                item => inputParameters.DecodeParameter<string>(item.Key));

            return new AzureSubmissionContext()
            {
                FriendlyName = jobName,
                Shots = shots,
                OperationName = operationName,
                InputParameters = decodedParameters,
                ExecutionTimeout = timeout,
                ExecutionPollingInterval = pollingInterval,
            };
        }
    }
}
