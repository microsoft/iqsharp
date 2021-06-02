namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <inheritdoc />
    public class AzureFactory : IAzureFactory
    {
        /// <inheritdoc />
        public Azure.Quantum.IWorkspace CreateWorkspace(string subscriptionId, string resourceGroup, string workspaceName, string location) =>
             new Azure.Quantum.Workspace(
                    subscriptionId: subscriptionId,
                    resourceGroupName: resourceGroup,
                    workspaceName: workspaceName,
                    location: location);
    }
}
