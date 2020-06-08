// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Quantum;
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

        internal static Dictionary<string, string> ToDictionary(this CloudJob cloudJob) =>
            new Dictionary<string, string>()
            {
                { "id", cloudJob.Id },
                { "name", cloudJob.Details.Name },
                { "status", cloudJob.Status },
                { "provider", cloudJob.Details.ProviderId },
                { "target", cloudJob.Details.Target },
            };

        internal static Table<CloudJob> ToJupyterTable(this IEnumerable<CloudJob> jobsList) =>
            new Table<CloudJob>
            {
                Columns = new List<(string, Func<CloudJob, string>)>
                    {
                        ("Job ID", cloudJob => cloudJob.Id),
                        ("Job Name", cloudJob => cloudJob.Details.Name),
                        ("Job Status", cloudJob => cloudJob.Status),
                        ("Provider", cloudJob => cloudJob.Details.ProviderId),
                        ("Target", cloudJob => cloudJob.Details.Target),
                    },
                Rows = jobsList.ToList()
            };

        internal static Dictionary<string, object> ToDictionary(this TargetStatus target) =>
            new Dictionary<string, object>()
            {
                { "targetName", target.Id },
                { "currentAvailability", target.CurrentAvailability },
                { "averageQueueTime", target.AverageQueueTime },
                { "statusPage", target.StatusPage },
            };

        internal static Table<TargetStatus> ToJupyterTable(this IEnumerable<TargetStatus> targets) =>
            new Table<TargetStatus>
            {
                Columns = new List<(string, Func<TargetStatus, string>)>
                    {
                        ("Target Name", target => target.Id),
                        ("Current Availability", target => target.CurrentAvailability),
                        ("Average Queue Time", target => target.AverageQueueTime.ToString()),
                        ("Status Page", target => target.StatusPage),
                    },
                Rows = targets.ToList()
            };
    }
}
