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
            ParamName_Login = "login",
            ParamName_StorageAccountConnectionString = "storageAccountConnectionString",
            ParamName_SubscriptionId = "subscriptionId",
            ParamName_ResourceGroupName = "resourceGroupName",
            ParamName_WorkspaceName = "workspaceName";

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
                            as specified by a valid connection string OR a valid combination of
                            subscription ID, resource group name, and workspace name.
                        ".Dedent(),
                    Examples = new[]
                        {
                            @"
                                Print information about the current connection:
                                ```
                                In []: %connect
                                Out[]: Connected to WORKSPACE_NAME
                                ```
                            ".Dedent(),

                            $@"
                                Connect to an Azure Quantum workspace using a connection string:
                                ```
                                In []: %connect {ParamName_StorageAccountConnectionString}=CONNECTION_STRING
                                Out[]: Connected to WORKSPACE_NAME
                                ```
                            ".Dedent(),

                            $@"
                                Connect to an Azure Quantum workspace and force a credential prompt:
                                ```
                                In []: %connect {ParamName_Login} {ParamName_StorageAccountConnectionString}=CONNECTION_STRING
                                Out[]: To sign in, use a web browser to open the page https://microsoft.com/devicelogin
                                        and enter the code [login code] to authenticate.
                                        Connected to WORKSPACE_NAME
                                ```
                                Use the `{ParamName_Login}` option if you want to bypass any saved or cached
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

            var storageAccountConnectionString = inputParameters.DecodeParameter<string>(ParamName_StorageAccountConnectionString);
            if (string.IsNullOrEmpty(storageAccountConnectionString))
            {
                return await AzureClient.PrintConnectionStatusAsync(channel);
            }

            var subscriptionId = inputParameters.DecodeParameter<string>(ParamName_SubscriptionId);
            var resourceGroupName = inputParameters.DecodeParameter<string>(ParamName_ResourceGroupName);
            var workspaceName = inputParameters.DecodeParameter<string>(ParamName_WorkspaceName);
            var forceLogin = inputParameters.DecodeParameter<bool>(ParamName_Login);
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