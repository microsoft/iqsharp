using Microsoft.Azure.Quantum;
using Microsoft.Quantum.Runtime;

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

        /// <inheritdoc />
        public IQuantumMachine CreateMachine(Azure.Quantum.IWorkspace workspace, string targetName, string storageConnectionString) =>
            QuantumMachineFactory.CreateMachine(workspace, targetName, storageConnectionString);
    }
}
