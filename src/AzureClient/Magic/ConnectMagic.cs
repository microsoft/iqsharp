// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Quantum.Authentication;
using Microsoft.Extensions.Logging;
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
        private const string ParameterNameLocation = "location";
        private const string ParameterNameCredential = "credential";

        private IConfigurationSource? config { get; }


        // A valid resource ID looks like:
        // /subscriptions/f846b2bd-d0e2-4a1d-8141-4c6944a9d387/resourceGroups/RESOURCE_GROUP_NAME/providers/Microsoft.Quantum/Workspaces/WORKSPACE_NAME
        private readonly static Regex ResourceIdRegex = new Regex(
            @"^/subscriptions/([a-fA-F0-9-]*)/resourceGroups/([^\s/]*)/providers/Microsoft\.Quantum/Workspaces/([^\s/]*)$",
            RegexOptions.IgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectMagic"/> class.
        /// </summary>
        /// <param name="azureClient">
        /// The <see cref="IAzureClient"/> object to use for Azure functionality.
        /// </param>
        /// <param name="config">Configuration Settings.</param>
        /// <param name="logger">Logger instance for messages.</param>
        public ConnectMagic(IAzureClient azureClient, IConfigurationSource config, ILogger<ConnectMagic> logger)
            : base(
                azureClient,
                "azure.connect",
                new Microsoft.Jupyter.Core.Documentation
                {
                    Summary = "Connects to an Azure Quantum workspace or displays current connection status.",
                    Description = $@"
                        This magic command allows for connecting to an Azure Quantum workspace
                        as specified by the resource ID and location of the workspace or by a combination of
                        subscription ID, resource group name, workspace name, and location.

                        If the connection is successful, a list of the available Q# execution targets
                        in the Azure Quantum workspace will be displayed.

                        #### Required parameters

                        The Azure Quantum workspace can be identified by resource ID:

                        - `{ParameterNameResourceId}=<string>`: The resource ID of the Azure Quantum workspace.
                        This can be obtained from the workspace page in the Azure portal. The `{ParameterNameResourceId}=` prefix
                        is optional for this parameter, as long as the resource ID is valid.

                        Alternatively, it can be identified by subscription ID, resource group name, and workspace name:
                        
                        - `{ParameterNameSubscriptionId}=<string>`: The Azure subscription ID for the Azure Quantum workspace.
                        - `{ParameterNameResourceGroupName}=<string>`: The Azure resource group name for the Azure Quantum workspace.
                        - `{ParameterNameWorkspaceName}=<string>`: The name of the Azure Quantum workspace.
                        
                        Along with the identifiers above, a valid location is required.

                        - `{ParameterNameLocation}=<string>`: The Azure region where the Azure Quantum workspace is provisioned.
                        This may be specified as a region name such as `""East US""` or a location name such as `""eastus""`.

                        #### Optional parameters

                        - `{ParameterNameStorageAccountConnectionString}=<string>`: The connection string to the Azure storage
                        account. Required if the specified Azure Quantum workspace was not linked to a storage
                        account at workspace creation time.
                        - `{ParameterNameCredential}=<CredentialType>`: The type of credentials to use to authenticate with Azure.
                        NOTE: to authenticate we leverage the [Azure Identity library](https://docs.microsoft.com/dotnet/api/overview/azure/identity-readme), 
                        based on this parameter we will create an instance of a Credential Class. 
                        Possible options are:
                          * [Environment](https://docs.microsoft.com/dotnet/api/azure.identity.environmentcredential):
                             Authenticates a service principal or user via credential information specified in environment variables.
                          * [ManagedIdentity](https://docs.microsoft.com/dotnet/api/azure.identity.managedidentitycredential):
                             Authenticates the managed identity of an azure resource.
                          * [CLI](https://docs.microsoft.com/dotnet/api/azure.identity.azureclicredential):
                             Authenticate in a development environment with the Azure CLI.
                          * [SharedToken](https://docs.microsoft.com/dotnet/api/azure.identity.sharedtokencachecredential):
                             Authenticate using tokens in the local cache shared between Microsoft applications.
                          * [VisualStudio](https://docs.microsoft.com/dotnet/api/azure.identity.visualstudiocredential):
                             Authenticate using data from Visual Studio.
                          * [VisualStudioCode](https://docs.microsoft.com/dotnet/api/azure.identity.visualstudiocodecredential):
                             Authenticate in a development environment with Visual Studio Code.
                          * [Interactive](https://docs.microsoft.com/dotnet/api/azure.identity.interactivebrowsercredential):
                             Opens a new browser window to interactively authenticate a user 
                             and obtain an access token.
                          * [DeviceCode](https://docs.microsoft.com/dotnet/api/azure.identity.devicecodecredential):
                             Authenticates a user using the device code flow to obtain an access token.
                        If not provided, it will try each credential type in order and pick the first one that can
                        succesfully authenticate with Azure.
                        
                        #### Possible errors

                        - {AzureClientError.WorkspaceNotFound.ToMarkdown()}
                        - {AzureClientError.AuthenticationFailed.ToMarkdown()}
                    ".Dedent(),
                    Examples = new[]
                    {
                        $@"
                            Connect to an Azure Quantum workspace using its resource ID to the 'West Us' region:
                            ```
                            In []: %azure.connect ""/subscriptions/.../Microsoft.Quantum/Workspaces/WORKSPACE_NAME"" {ParameterNameLocation}=""West US""
                            Out[]: Connected to Azure Quantum workspace WORKSPACE_NAME in location westus.
                                    <list of Q# execution targets available in the Azure Quantum workspace>
                            ```
                        ".Dedent(),

                        $@"
                            Connect to an Azure Quantum workspace using its resource ID, a storage account connection string, and a location:
                            ```
                            In []: %azure.connect {ParameterNameResourceId}=""/subscriptions/.../Microsoft.Quantum/Workspaces/WORKSPACE_NAME""
                                                  {ParameterNameStorageAccountConnectionString}=""STORAGE_ACCOUNT_CONNECTION_STRING""
                                                  {ParameterNameLocation}=""East US""
                            Out[]: Connected to Azure Quantum workspace WORKSPACE_NAME in location eastus.
                                    <list of Q# execution targets available in the Azure Quantum workspace>
                            ```
                        ".Dedent(),

                        $@"
                            Connect to an Azure Quantum workspace using individual subscription ID, resource group name, using a browser to prompt for user credentials with Azure:
                            ```
                            In []: %azure.connect {ParameterNameSubscriptionId}=""SUBSCRIPTION_ID""
                                                  {ParameterNameResourceGroupName}=""RESOURCE_GROUP_NAME""
                                                  {ParameterNameWorkspaceName}=""WORKSPACE_NAME""
                                                  {ParameterNameLocation}=""West US""
                                                  {ParameterNameCredential}=""interactive""
                            Out[]: Connected to Azure Quantum workspace WORKSPACE_NAME in location westus.
                                    <list of Q# execution targets available in the Azure Quantum workspace>
                            ```
                        ".Dedent(),

                        @"
                            Print information about the currently-connected Azure Quantum workspace:
                            ```
                            In []: %azure.connect
                            Out[]: Connected to Azure Quantum workspace WORKSPACE_NAME in location westus.
                                    <list of Q# execution targets available in the Azure Quantum workspace>
                            ```
                        ".Dedent(),
                    },
                },
                logger)
        {
            this.config = config;
        }

        /// <summary>
        ///     Connects to an Azure workspace given a subscription ID, resource group name,
        ///     workspace name, and connection string as a JSON-encoded object.
        /// </summary>
        public override async Task<ExecutionResult> RunAsync(string input, IChannel channel, CancellationToken cancellationToken)
        {
            var inputParameters = ParseInputParameters(input);
            if (!inputParameters.Any())
            {
                return await AzureClient.GetConnectionStatusAsync(channel, cancellationToken);
            }

            var resourceId = inputParameters.DecodeParameter<string>(ParameterNameResourceId, defaultValue: string.Empty);
            var subscriptionId = string.Empty;
            var resourceGroupName = string.Empty;
            var workspaceName = string.Empty;

            if (string.IsNullOrEmpty(resourceId))
            {
                resourceId = inputParameters.Keys.FirstOrDefault(key => ResourceIdRegex.IsMatch(key)) ?? string.Empty;
            }

            var match = ResourceIdRegex.Match(resourceId);
            if (match.Success)
            {
                // match.Groups will be a GroupCollection containing four Group objects:
                // -> match.Groups[0]: The full resource ID for the Azure Quantum workspace
                // -> match.Groups[1]: The Azure subscription ID
                // -> match.Groups[2]: The Azure resource group name
                // -> match.Groups[3]: The Azure Quantum workspace name
                subscriptionId = match.Groups[1].Value;
                resourceGroupName = match.Groups[2].Value;
                workspaceName = match.Groups[3].Value;
            }
            else
            {
                // look for each of the parameters individually
                subscriptionId = inputParameters.DecodeParameter<string>(ParameterNameSubscriptionId, defaultValue: config?.SubscriptionId ?? string.Empty);
                resourceGroupName = inputParameters.DecodeParameter<string>(ParameterNameResourceGroupName, defaultValue: config?.WorkspaceRG ?? string.Empty);
                workspaceName = inputParameters.DecodeParameter<string>(ParameterNameWorkspaceName, defaultValue: config?.WorkspaceName ?? string.Empty);
            }

            if (string.IsNullOrWhiteSpace(subscriptionId) ||
                string.IsNullOrWhiteSpace(resourceGroupName) ||
                string.IsNullOrWhiteSpace(workspaceName))
            {
                channel.Stderr($"Please specify a valid {ParameterNameResourceId}, or specify a valid combination of " +
                    $"{ParameterNameSubscriptionId}, {ParameterNameResourceGroupName}, and {ParameterNameWorkspaceName}.");
                return AzureClientError.WorkspaceNotFound.ToExecutionResult();
            }

            var location = inputParameters.DecodeParameter<string>(ParameterNameLocation, defaultValue: config?.WorkspaceLocation ?? string.Empty);
            var storageAccountConnectionString = inputParameters.DecodeParameter<string>(ParameterNameStorageAccountConnectionString, defaultValue: string.Empty);
            var credentialType = inputParameters.DecodeParameter<CredentialType>(ParameterNameCredential, defaultValue: CredentialType.Default);
            var updatableDisplay = channel.DisplayUpdatable("Connecting to Azure Quantum...");
            try
            {
                return await AzureClient.ConnectAsync(
                    channel,
                    subscriptionId,
                    resourceGroupName,
                    workspaceName,
                    storageAccountConnectionString,
                    location,
                    credentialType,
                    cancellationToken);
            }
            finally
            {
                updatableDisplay.Update("");
            }
        }
    }
}
