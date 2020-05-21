// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
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
        private const string
            ParameterNameLogin = "login",
            ParameterNameStorageAccountConnectionString = "storageAccountConnectionString",
            ParameterNameSubscriptionId = "subscriptionId",
            ParameterNameResourceGroupName = "resourceGroupName",
            ParameterNameWorkspaceName = "workspaceName";

        /// <summary>
        ///     Constructs a new magic command given an IAzureClient object.
        /// </summary>
        public ConnectMagic(IAzureClient azureClient) :
            base(azureClient,
                "connect",
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
                                In []: %connect
                                Out[]: Connected to Azure Quantum workspace WORKSPACE_NAME.
                                       <list of targets available in the Azure Quantum workspace>
                                ```
                            ".Dedent(),

                            $@"
                                Connect to an Azure Quantum workspace:
                                ```
                                In []: %connect {ParameterNameSubscriptionId}=SUBSCRIPTION_ID
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
                                In []: %connect {ParameterNameLogin}
                                                {ParameterNameSubscriptionId}=SUBSCRIPTION_ID
                                                {ParameterNameResourceGroupName}=RESOURCE_GROUP_NAME
                                                {ParameterNameWorkspaceName}=WORKSPACE_NAME
                                                {ParameterNameStorageAccountConnectionString}=CONNECTION_STRING
                                Out[]: To sign in, use a web browser to open the page https://microsoft.com/devicelogin
                                        and enter the code [login code] to authenticate.
                                       Connected to Azure Quantum workspace WORKSPACE_NAME.
                                       <list of targets available in the Azure Quantum workspace>
                                ```
                                Use the `{ParameterNameLogin}` option if you want to bypass any saved or cached
                                credentials when connecting to Azure.
                            ".Dedent()
                        }
                }) {}

        /// <summary>
        ///     Connects to an Azure workspace given a subscription ID, resource group name,
        ///     workspace name, and connection string as a JSON-encoded object.
        /// </summary>
        public override async Task<ExecutionResult> RunAsync(string input, IChannel channel)
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
            var forceLogin = inputParameters.DecodeParameter<bool>(ParameterNameLogin, defaultValue: false);
            return await AzureClient.ConnectAsync(
                channel,
                subscriptionId,
                resourceGroupName,
                workspaceName,
                storageAccountConnectionString,
                forceLogin);
        }
    }
}