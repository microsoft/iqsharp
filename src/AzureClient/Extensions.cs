// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
        ///     Parses the input parameters for a given magic symbol and returns a
        ///     <c>Dictionary</c> with the names and values of the parameters.
        /// </summary>
        public static Dictionary<string, string> ParseInput(this MagicSymbol magic, string input)
        {
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
            foreach (string arg in input.Split(null as char[], StringSplitOptions.RemoveEmptyEntries))
            {
                var tokens = arg.Split("=", 2);
                var key = tokens[0].Trim();
                var value = (tokens.Length == 1) ? string.Empty : tokens[1].Trim();
                keyValuePairs[key] = value;
            }
            return keyValuePairs;
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
                Status = azureClientError == AzureClientError.Success ? ExecuteStatus.Ok : ExecuteStatus.Error,
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
    }
}
