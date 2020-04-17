// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Jupyter.Core;
using System.Threading.Tasks;

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using System.Linq;
using System.IO;
using Microsoft.Quantum.Runtime;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <inheritdoc/>
    public class AzureClient : IAzureClient
    {
        /// <summary>
        /// Creates an AzureClient object.
        /// </summary>
        public AzureClient()
        {
        }

        /// <inheritdoc/>
        public async Task<AzureClientError> ConnectAsync(
            IChannel channel,
            string subscriptionId,
            string resourceGroupName,
            string workspaceName,
            string storageAccountConnectionString,
            bool forceLogin = false)
        {
            return AzureClientError.UnknownError;
        }

        /// <inheritdoc/>
        public async Task<AzureClientError> PrintConnectionStatusAsync(IChannel channel)
        {
            return AzureClientError.UnknownError;
        }

        /// <inheritdoc/>
        public async Task<AzureClientError> SubmitJobAsync(
            IChannel channel,
            IOperationResolver operationResolver,
            string operationName)
        {
            return AzureClientError.UnknownError;
        }

        /// <inheritdoc/>
        public async Task<AzureClientError> SetActiveTargetAsync(
            IChannel channel,
            string targetName)
        {
            return AzureClientError.UnknownError;
        }

        /// <inheritdoc/>
        public async Task<AzureClientError> PrintActiveTargetAsync(
            IChannel channel)
        {
            return AzureClientError.UnknownError;
        }

        /// <inheritdoc/>
        public async Task<AzureClientError> PrintTargetListAsync(
            IChannel channel)
        {
            return AzureClientError.UnknownError;
        }

        /// <inheritdoc/>
        public async Task<AzureClientError> PrintJobStatusAsync(
            IChannel channel,
            string jobId)
        {
            return AzureClientError.UnknownError;
        }

        /// <inheritdoc/>
        public async Task<AzureClientError> PrintJobListAsync(
            IChannel channel)
        {
            return AzureClientError.UnknownError;
        }
    }
}
