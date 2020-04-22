// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    ///     A magic command that can be used to connect to an Azure workspace.
    /// </summary>
    public class ConnectMagic : AzureClientMagicBase
    {
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

                            @"
                                Connect to an Azure Quantum workspace using a connection string:
                                ```
                                In []: %connect connectionString=CONNECTION_STRING
                                Out[]: Connected to WORKSPACE_NAME
                                ```
                            ".Dedent(),

                            @"
                                Connect to an Azure Quantum workspace and force a credential prompt:
                                ```
                                In []: %connect login connectionString=CONNECTION_STRING
                                Out[]: To sign in, use a web browser to open the page https://microsoft.com/devicelogin
                                        and enter the code [login code] to authenticate.
                                        Connected to WORKSPACE_NAME
                                ```
                                Use the `login` option if you want to bypass any saved or cached
                                credentials when connecting to Azure.
                            ".Dedent()
                        }
                }) {}

        /// <summary>
        ///     Connects to an Azure workspace given a subscription ID, resource group name,
        ///     workspace name, and connection string as a JSON-encoded object.
        /// </summary>
        public override async Task<AzureClientError> RunAsync(string input, IChannel channel)
        {
            Dictionary<string, string> keyValuePairs = this.ParseInput(input);

            string storageAccountConnectionString;
            keyValuePairs.TryGetValue("storageAccountConnectionString", out storageAccountConnectionString);
            if (string.IsNullOrEmpty(storageAccountConnectionString))
            {
                return await AzureClient.PrintConnectionStatusAsync(channel);
            }

            string subscriptionId, resourceGroupName, workspaceName;
            keyValuePairs.TryGetValue("subscriptionId", out subscriptionId);
            keyValuePairs.TryGetValue("resourceGroupName", out resourceGroupName);
            keyValuePairs.TryGetValue("workspaceName", out workspaceName);

            bool forceLogin = keyValuePairs.ContainsKey("login");
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