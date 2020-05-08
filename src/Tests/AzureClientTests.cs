// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.AzureClient;

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
