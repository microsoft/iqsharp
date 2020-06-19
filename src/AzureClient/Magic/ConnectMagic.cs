// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Jupyter;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    ///     A magic command that can be used to connect to an Azure workspace.
    /// </summary>
    public class ConnectMagic : AzureClientMagicBase
    {
        private const string ParameterNameRefresh = "refresh";
        private const string ParameterNameStorageAccountConnectionString = "storageAccountConnectionString";
        private const string ParameterNameSubscriptionId = "subscriptionId";
        private const string ParameterNameResourceGroupName = "resourceGroupName";
        private const string ParameterNameWorkspaceName = "workspaceName";

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectMagic"/> class.
        /// </summary>
        /// <param name="azureClient">
        /// The <see cref="IAzureClient"/> object to use for Azure functionality.
        /// </param>
        public ConnectMagic(IAzureClient azureClient)
            : base(
                azureClient,
                "azure.connect",
                new Documentation
                {
                    Summary = "Connects to an Azure workspace or displays current connection status.",
                    Description = @"
                            This magic command allows for connecting to an Azure Quantum workspace
                            as specified by a valid subscription ID, resource group name, workspace name,
                            and storage account connection string.

                            If the connection is successful, a list of the available execution targets
                            in the Azure Quantum workspace will be displayed.
                        ".Dedent(),
                    Examples = new[]
                        {
                            @"
                                Print information about the current connection:
                                ```
                                In []: %azure.connect
                                Out[]: Connected to Azure Quantum workspace WORKSPACE_NAME.
                                       <list of targets available in the Azure Quantum workspace>
                                ```
                            ".Dedent(),

                            $@"
                                Connect to an Azure Quantum workspace:
                                ```
                                In []: %azure.connect {ParameterNameSubscriptionId}=SUBSCRIPTION_ID
                                                {ParameterNameResourceGroupName}=RESOURCE_GROUP_NAME
                                                {ParameterNameWorkspaceName}=WORKSPACE_NAME
                                                {ParameterNameStorageAccountConnectionString}=CONNECTION_STRING
                                Out[]: Connected to Azure Quantum workspace WORKSPACE_NAME.
                                       <list of targets available in the Azure Quantum workspace>
                                ```
                            ".Dedent(),

                            $@"
                                Connect to an Azure Quantum workspace and force a credential prompt:
                                ```
                                In []: %azure.connect {ParameterNameRefresh}
                                                {ParameterNameSubscriptionId}=SUBSCRIPTION_ID
                                                {ParameterNameResourceGroupName}=RESOURCE_GROUP_NAME
                                                {ParameterNameWorkspaceName}=WORKSPACE_NAME
                                                {ParameterNameStorageAccountConnectionString}=CONNECTION_STRING
                                Out[]: To sign in, use a web browser to open the page https://microsoft.com/devicelogin
                                        and enter the code [login code] to authenticate.
                                       Connected to Azure Quantum workspace WORKSPACE_NAME.
                                       <list of targets available in the Azure Quantum workspace>
                                ```
                                Use the `{ParameterNameRefresh}` option if you want to bypass any saved or cached
                                credentials when connecting to Azure.
                            ".Dedent(),
                        },
                }) {}

        /// <summary>
        ///     Connects to an Azure workspace given a subscription ID, resource group name,
        ///     workspace name, and connection string as a JSON-encoded object.
        /// </summary>
        public override async Task<ExecutionResult> RunAsync(string input, IChannel channel, CancellationToken cancellationToken)
        {
            var inputParameters = ParseInputParameters(input);

            var storageAccountConnectionString = inputParameters.DecodeParameter<string>(ParameterNameStorageAccountConnectionString);
            if (string.IsNullOrEmpty(storageAccountConnectionString))
            {
                return await AzureClient.GetConnectionStatusAsync(channel);
            }

            var subscriptionId = inputParameters.DecodeParameter<string>(ParameterNameSubscriptionId);
            var resourceGroupName = inputParameters.DecodeParameter<string>(ParameterNameResourceGroupName);
            var workspaceName = inputParameters.DecodeParameter<string>(ParameterNameWorkspaceName);
            var refreshCredentials = inputParameters.DecodeParameter<bool>(ParameterNameRefresh, defaultValue: false);
            return await AzureClient.ConnectAsync(
                channel,
                subscriptionId,
                resourceGroupName,
                workspaceName,
                storageAccountConnectionString,
                refreshCredentials);
        }
    }
}