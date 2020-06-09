// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.Runtime;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    public sealed class AzureSubmissionContext : IQuantumMachineSubmissionContext
    {
        private static int defaultShots = 500;

        public string FriendlyName { get; set; } = string.Empty;

        public int Shots { get; set; } = defaultShots;

        public string OperationName { get; set; } = string.Empty;

        public Dictionary<string, string> InputParameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        ///     Parses the input from a magic command into an <see cref="AzureSubmissionContext"/> object
        ///     suitable for job submission via <see cref="IAzureClient"/>.
        /// </summary>
        public static AzureSubmissionContext Parse(string inputCommand)
        {
            var parameterNameOperationName = "operationName";
            var parameterNameJobName = "jobName";
            var parameterNameShots = "shots";

            var inputParameters = AbstractMagic.ParseInputParameters(inputCommand, firstParameterInferredName: parameterNameOperationName);
            var operationName = inputParameters.DecodeParameter<string>(parameterNameOperationName);
            var jobName = inputParameters.DecodeParameter<string>(parameterNameJobName, defaultValue: operationName);
            var shots = inputParameters.DecodeParameter<int>(parameterNameShots, defaultValue: defaultShots);

            var decodedParameters = new Dictionary<string, string>();
            foreach (var key in inputParameters.Keys)
            {
                decodedParameters[key] = inputParameters.DecodeParameter<string>(key);
            }

            return new AzureSubmissionContext()
            {
                FriendlyName = jobName,
                Shots = shots,
                OperationName = operationName,
                InputParameters = decodedParameters,
            };
        }
    }
}
