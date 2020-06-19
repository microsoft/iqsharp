// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        private const string ParameterNameStorageAccountConnectionString = "storage";
        private const string ParameterNameSubscriptionId = "subscription";
        private const string ParameterNameResourceGroupName = "resourceGroup";
        private const string ParameterNameWorkspaceName = "workspace";
        private const string ParameterNameResourceId = "resourceId";

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
                    Summary = "Connects to an Azure Quantum workspace or displays current connection status.",
                    Description = @"
                            This magic command allows for connecting to an Azure Quantum workspace
                            as specified by the resource ID of the workspace or by a combination of
                            subscription ID, resource group name, and workspace name.

                            If the connection is successful, a list of the available Q# execution targets
                            in the Azure Quantum workspace will be displayed.
                        ".Dedent(),
                    Examples = new[]
                        {
                            $@"
                                Connect to an Azure Quantum workspace using its resource ID:
                                ```
                                In []: %azure.connect {ParameterNameResourceId}=""/subscriptions/f846b2bd-d0e2-4a1d-8141-4c6944a9d387/resourceGroups/RESOURCE_GROUP_NAME/providers/Microsoft.Quantum/Workspaces/WORKSPACE_NAME""
                                Out[]: Connected to Azure Quantum workspace WORKSPACE_NAME.
                                       <list of Q# execution targets available in the Azure Quantum workspace>
                                ```
                            ".Dedent(),

                            $@"
                                Connect to an Azure Quantum workspace using individual parameters:
                                ```
                                In []: %azure.connect {ParameterNameSubscriptionId}=""SUBSCRIPTION_ID""
                                                      {ParameterNameResourceGroupName}=""RESOURCE_GROUP_NAME""
                                                      {ParameterNameWorkspaceName}=""WORKSPACE_NAME""
                                                      {ParameterNameStorageAccountConnectionString}=""STORAGE_ACCOUNT_CONNECTION_STRING""
                                Out[]: Connected to Azure Quantum workspace WORKSPACE_NAME.
                                       <list of Q# execution targets available in the Azure Quantum workspace>
                                ```
                                The `{ParameterNameStorageAccountConnectionString}` parameter is necessary only if the
                                specified Azure Quantum workspace was not linked to a storage account at creation time.
                            ".Dedent(),

                            $@"
                                Connect to an Azure Quantum workspace and force a credential prompt using
                                the `{ParameterNameRefresh}` option:
                                ```
                                In []: %azure.connect {ParameterNameRefresh} {ParameterNameResourceId}=""/subscriptions/f846b2bd-d0e2-4a1d-8141-4c6944a9d387/resourceGroups/RESOURCE_GROUP_NAME/providers/Microsoft.Quantum/Workspaces/WORKSPACE_NAME""
                                Out[]: To sign in, use a web browser to open the page https://microsoft.com/devicelogin
                                        and enter the code [login code] to authenticate.
                                       Connected to Azure Quantum workspace WORKSPACE_NAME.
                                       <list of Q# execution targets available in the Azure Quantum workspace>
                                ```
                                The `{ParameterNameRefresh}` option bypasses any saved or cached
                                credentials when connecting to Azure.
                            ".Dedent(),

                            @"
                                Print information about the current connection:
                                ```
                                In []: %azure.connect
                                Out[]: Connected to Azure Quantum workspace WORKSPACE_NAME.
                                       <list of Q# execution targets available in the Azure Quantum workspace>
                                ```
                            ".Dedent(),
                        },
                }) {}

        /// <summary>
        ///     Connects to an Azure workspace given a subscription ID, resource group name,
        ///     workspace name, and connection string as a JSON-encoded object.
        /// </summary>
        public override async Task<ExecutionResult> RunAsync(string input, IChannel channel)
        {
            var inputParameters = ParseInputParameters(input);
            if (!inputParameters.Any())
            {
                return await AzureClient.GetConnectionStatusAsync(channel);
            }

            var resourceId = inputParameters.DecodeParameter<string>(ParameterNameResourceId, defaultValue: string.Empty);
            var subscriptionId = string.Empty;
            var resourceGroupName = string.Empty;
            var workspaceName = string.Empty;

            // A valid resource ID looks like:
            // /subscriptions/f846b2bd-d0e2-4a1d-8141-4c6944a9d387/resourceGroups/RESOURCE_GROUP_NAME/providers/Microsoft.Quantum/Workspaces/WORKSPACE_NAME
            var resourceIdRegex = new Regex(
                @"^\/subscriptions\/([a-zA-Z0-9\-]*)\/resourceGroups\/([^\s\/]*)\/providers\/Microsoft\.Quantum\/Workspaces\/([^\s\/]*)$");
            var match = resourceIdRegex.Match(resourceId);
            if (match.Success)
            {
                // match.Groups will be a GroupCollection containing four Group objects:
                // -> match.Groups[0]: The full resource ID for the Azure Quantum workspace
                // -> match.Groups[1]: The Azure subscription ID
                // -> match.Groups[2]: The Azure resource group name
                // -> match.Groups[3]: The Azure Quantum workspace name
                var match = resourceIdRegex.Match(resourceId);
                subscriptionId = match.Groups[1].Value;
                resourceGroupName = match.Groups[2].Value;
                workspaceName = match.Groups[3].Value;
            }
            else
            {
                // look for each of the parameters individually
                subscriptionId = inputParameters.DecodeParameter<string>(ParameterNameSubscriptionId, defaultValue: string.Empty);
                resourceGroupName = inputParameters.DecodeParameter<string>(ParameterNameResourceGroupName, defaultValue: string.Empty);
                workspaceName = inputParameters.DecodeParameter<string>(ParameterNameWorkspaceName, defaultValue: string.Empty);
            }

            var storageAccountConnectionString = inputParameters.DecodeParameter<string>(ParameterNameStorageAccountConnectionString, defaultValue: string.Empty);
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
