// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Quantum.IQSharp.AzureClient;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Jupyter.Core;
using System.Threading.Tasks;
using Microsoft.Quantum.IQSharp;
using Microsoft.Azure.Quantum.DataPlane.Client;
using Microsoft.Azure.Quantum.DataPlane.Client.Models;
using Microsoft.Azure.Quantum.ResourceManager.Client;
using Microsoft.Azure.Quantum.ResourceManager.Client.Models;
using Moq;

namespace Tests.IQSharp
{
    public static class AzureClientTestExtensions
    {
    }

    [TestClass]
    public class AzureClientTests
    {
        private readonly string subscriptionId = "TEST_SUBSCRIPTION_ID";
        private readonly string resourceGroupName = "TEST_RESOURCE_GROUP_NAME";
        private readonly string workspaceName = "TEST_WORKSPACE_NAME";
        private readonly string storageAccountConnectionString = "TEST_CONNECTION_STRING";
        private readonly string jobId = "TEST_JOB_ID";
        private readonly string operationName = "TEST_OPERATION_NAME";
        private readonly string targetName = "TEST_TARGET_NAME";

        [TestMethod]
        public void TestTargets()
        {
            var azureClient = new AzureClient();

            var mockQuantumClient = new Mock<IQuantumClient>();
            var mockQuantumManagementClient = new Mock<IQuantumManagementClient>();

            azureClient.InjectRestClients(mockQuantumClient.Object, mockQuantumManagementClient.Object);

            var result = azureClient.SetActiveTargetAsync(new MockChannel(), targetName).GetAwaiter().GetResult();
            Assert.IsTrue(result.Status == ExecuteStatus.Ok);

            result = azureClient.PrintActiveTargetAsync(new MockChannel()).GetAwaiter().GetResult();
            Assert.IsTrue(result.Status == ExecuteStatus.Ok);
            Assert.IsNotNull(result.Output);

            result = azureClient.PrintTargetListAsync(new MockChannel()).GetAwaiter().GetResult();
            Assert.IsTrue(result.Status == ExecuteStatus.Error);
        }
    }
}
