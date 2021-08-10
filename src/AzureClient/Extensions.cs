﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Azure.Quantum;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    ///      Extension methods to be used with various IQ# and AzureClient objects.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        ///     The name of the environment variable used by Python tests
        ///     to indicate the Azure Environment to use.
        /// </summary>
        public static string AZURE_QUANTUM_ENV = "AZURE_QUANTUM_ENV";

        /// <summary>
        ///    Value of the environment variable used by Python tests
        ///     to indicate to use Mocks for Azure Quantum instances.
        /// </summary>
        public static string MOCK_ENVIRONMENT = "mock";

        /// <summary>
        ///     Adds services required for the AzureClient to a given service collection.
        /// </summary>
        public static void AddAzureClient(this IServiceCollection services)
        {
            services.AddSingleton<IAzureClient, AzureClient>();
            services.AddSingleton<IEntryPointGenerator, EntryPointGenerator>();

            // For Python unittests, we support an environment variable to create Mock Azure objects:
            var useMocks = System.Environment.GetEnvironmentVariable(AZURE_QUANTUM_ENV) == MOCK_ENVIRONMENT;
            services.AddSingleton(useMocks ? new MocksAzureFactory() : new AzureFactory() as IAzureFactory);
        }

        /// <summary>
        ///      Encapsulates a given <see cref="AzureClientError"/> as the result of an execution.
        /// </summary>
        /// <param name="azureClientError">
        ///      The result of an IAzureClient API call.
        /// </param>
        internal static ExecutionResult ToExecutionResult(this AzureClientError azureClientError) =>
            new ExecutionResult
            {
                Status = ExecuteStatus.Error,
                Output = azureClientError,
            };

        /// <summary>
        ///      Encapsulates a given <see cref="AzureClientError"/> as the result of an execution.
        /// </summary>
        /// <param name="task">
        ///      A task which will return the result of an IAzureClient API call.
        /// </param>
        internal static async Task<ExecutionResult> ToExecutionResult(this Task<AzureClientError> task) =>
            (await task).ToExecutionResult();

        /// <summary>
        ///     Returns the provided argument as an enumeration of the specified type.
        /// </summary>
        /// <returns>
        ///     If the argument is already an <see cref="IEnumerable{T}"/> of the specified type,
        ///     the argument is returned. If the argument is of type <c>T</c>, then an 
        ///     enumeration is returned with this argument as the only element.
        ///     Otherwise, null is returned.
        /// </returns>
        internal static IEnumerable<T>? AsEnumerableOf<T>(this object? source) =>
            source is T singleton ? new List<T> { singleton } :
            source is IEnumerable<T> collection ? collection :
            null;

        /// <summary>
        ///     Determines whether the given <see cref="CloudJob"/> matches the given <c>filter</c>.
        /// </summary>
        internal static bool Matches(this CloudJob job, string filter) =>
            (job.Id != null && job.Id.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
            (job.Details.Name != null && job.Details.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
            (job.Details.Target != null && job.Details.Target.Contains(filter, StringComparison.OrdinalIgnoreCase));

        internal static void Log(this Exception ex, IChannel? channel, ILogger? logger, string msg = "")
        {
            logger?.LogError(ex, msg);
            channel?.Stderr(msg);
            channel?.Stderr(ex.Message);
        }
    }
}
