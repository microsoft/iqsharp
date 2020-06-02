// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Quantum.Client;
using Microsoft.Azure.Quantum.Client.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.Runtime;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    ///      Extension methods to be used with various IQ# and AzureClient objects.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        ///     Adds services required for the AzureClient to a given service collection.
        /// </summary>
        public static void AddAzureClient(this IServiceCollection services)
        {
            services.AddSingleton<IAzureClient, AzureClient>();
            services.AddSingleton<IEntryPointGenerator, EntryPointGenerator>();
        }

        /// <summary>
        ///      Encapsulates a given <see cref="AzureClientError"/> as the result of an execution.
        /// </summary>
        /// <param name="azureClientError">
        ///      The result of an IAzureClient API call.
        /// </param>
        public static ExecutionResult ToExecutionResult(this AzureClientError azureClientError) =>
            new ExecutionResult
            {
                Status = ExecuteStatus.Error,
                Output = azureClientError.ToDescription()
            };

        /// <summary>
        ///     Returns the string value of the <see cref="DescriptionAttribute"/> for the given
        ///     <see cref="AzureClientError"/> enumeration value.
        /// </summary>
        /// <param name="azureClientError"></param>
        /// <returns></returns>
        public static string ToDescription(this AzureClientError azureClientError)
        {
            var attributes = azureClientError
                .GetType()
                .GetField(azureClientError.ToString())
                .GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
            return attributes?.Length > 0 ? attributes[0].Description : string.Empty;
        }

        /// <summary>
        ///      Encapsulates a given <see cref="AzureClientError"/> as the result of an execution.
        /// </summary>
        /// <param name="task">
        ///      A task which will return the result of an IAzureClient API call.
        /// </param>
        public static async Task<ExecutionResult> ToExecutionResult(this Task<AzureClientError> task) =>
            (await task).ToExecutionResult();

        internal static Table<JobDetails> ToJupyterTable(this JobDetails jobDetails) =>
            new List<JobDetails> { jobDetails }.ToJupyterTable();

        internal static Table<JobDetails> ToJupyterTable(this IEnumerable<JobDetails> jobsList) =>
            new Table<JobDetails>
            {
                Columns = new List<(string, Func<JobDetails, string>)>
                    {
                        ("JobId", jobDetails => jobDetails.Id),
                        ("JobName", jobDetails => jobDetails.Name),
                        ("JobStatus", jobDetails => jobDetails.Status),
                        ("Provider", jobDetails => jobDetails.ProviderId),
                        ("Target", jobDetails => jobDetails.Target),
                    },
                Rows = jobsList.ToList()
            };

        internal static Table<IQuantumMachineJob> ToJupyterTable(this IQuantumMachineJob job) =>
            new Table<IQuantumMachineJob>
            {
                Columns = new List<(string, Func<IQuantumMachineJob, string>)>
                    {
                        ("JobId", job => job.Id),
                        ("JobStatus", job => job.Status),
                    },
                Rows = new List<IQuantumMachineJob>() { job }
            };

        internal static Table<IQuantumClient> ToJupyterTable(this IQuantumClient quantumClient) =>
            new Table<IQuantumClient>
            {
                Columns = new List<(string, Func<IQuantumClient, string>)>
                    {
                        ("SubscriptionId", quantumClient => quantumClient.SubscriptionId),
                        ("ResourceGroupName", quantumClient => quantumClient.ResourceGroupName),
                        ("WorkspaceName", quantumClient => quantumClient.WorkspaceName),
                    },
                Rows = new List<IQuantumClient>() { quantumClient }
            };

        internal static Table<TargetStatus> ToJupyterTable(this IEnumerable<TargetStatus> targets) =>
            new Table<TargetStatus>
            {
                Columns = new List<(string, Func<TargetStatus, string>)>
                    {
                        ("TargetId", target => target.Id),
                        ("CurrentAvailability", target => target.CurrentAvailability),
                        ("AverageQueueTime", target => target.AverageQueueTime.ToString()),
                        ("StatusPage", target => target.StatusPage),
                    },
                Rows = targets.ToList()
            };
    }
}
