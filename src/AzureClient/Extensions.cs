// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Quantum.Client.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Jupyter.Core;

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
        }

        /// <summary>
        ///      Encapsulates a given <c>AzureClientError</c> as the result of an execution.
        /// </summary>
        /// <param name="azureClientError">
        ///      The result of an IAzureClient API call.
        /// </param>
        public static ExecutionResult ToExecutionResult(this AzureClientError azureClientError) =>
            new ExecutionResult
            {
                Status = ExecuteStatus.Error,
                Output = azureClientError
            };

        /// <summary>
        ///      Encapsulates a given <c>AzureClientError</c> as the result of an execution.
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
                        ("Id", jobDetails => jobDetails.Id),
                        ("ProviderId", jobDetails => jobDetails.ProviderId),
                        ("Status", jobDetails => jobDetails.Status)
                    },
                Rows = jobsList.ToList()
            };

        internal static Table<Azure.Quantum.IWorkspace> ToJupyterTable(this Azure.Quantum.IWorkspace workspace) =>
            new List<Azure.Quantum.IWorkspace> { workspace }.ToJupyterTable();

        internal static Table<Azure.Quantum.IWorkspace> ToJupyterTable(this IEnumerable<Azure.Quantum.IWorkspace> workspacesList) =>
            new Table<Azure.Quantum.IWorkspace>
            {
                Columns = new List<(string, Func<Azure.Quantum.IWorkspace, string>)>
                    {
                        // TODO: Uncomment this after updating the Azure.Quantum.Client API
                        //("Name", workspace => workspace.Name),
                        //("Type", workspace => workspace.Type),
                        //("Location", workspace => workspace.Location),
                        ("TEMP_AsString", workspace => workspace.ToString()),
                    },
                Rows = workspacesList.ToList()
            };

        // TODO: Implement this for providers and targets once they are exposed
        //       through the Azure.Quantum.Client API.

        //internal static Table<Provider> ToJupyterTable(this Provider provider) =>
        //    new List<Provider> { provider }.ToJupyterTable();

        //internal static Table<Provider> ToJupyterTable(this IEnumerable<Provider> providersList) =>
        //    new Table<Provider>
        //    {
        //        Columns = new List<(string, Func<Provider, string>)>
        //            {
        //                ("Name", provider => provider.ApplicationName),
        //                ("ProviderId", provider => provider.ProviderId),
        //                ("ProviderSku", provider => provider.ProviderSku),
        //                ("ProvisioningState", provider => provider.ProvisioningState)
        //            },
        //        Rows = providersList.ToList()
        //    };
    }
}
