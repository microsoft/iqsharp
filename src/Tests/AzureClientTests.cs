// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.AzureClient;
using Microsoft.Extensions.DependencyInjection;

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

        [TestMethod]
        public void TestTargets()
        {
            var workspace = "Workspace";
            var services = Startup.CreateServiceProvider(workspace);
            var azureClient = services.GetService<IAzureClient>();

            // SetActiveTargetAsync with recognized target ID, but not yet connected
            var result = azureClient.SetActiveTargetAsync(new MockChannel(), "ionq.simulator").GetAwaiter().GetResult();
            Assert.IsTrue(result.Status == ExecuteStatus.Error);

            // SetActiveTargetAsync with unrecognized target ID
            result = azureClient.SetActiveTargetAsync(new MockChannel(), "contoso.qpu").GetAwaiter().GetResult();
            Assert.IsTrue(result.Status == ExecuteStatus.Error);

            // GetActiveTargetAsync, but not yet connected
            result = azureClient.GetActiveTargetAsync(new MockChannel()).GetAwaiter().GetResult();
            Assert.IsTrue(result.Status == ExecuteStatus.Error);
        }

        [TestMethod]
        public void TestAzureExecutionTarget()
        {
            var targetId = "invalidname";
            var executionTarget = AzureExecutionTarget.Create(targetId);
            Assert.IsNull(executionTarget);

            targetId = "ionq.targetId";
            executionTarget = AzureExecutionTarget.Create(targetId);
            Assert.IsNotNull(executionTarget);
            Assert.AreEqual(executionTarget.TargetId, targetId);
            Assert.AreEqual(executionTarget.PackageName, "Microsoft.Quantum.Providers.IonQ");

            targetId = "HonEYWEll.targetId";
            executionTarget = AzureExecutionTarget.Create(targetId);
            Assert.IsNotNull(executionTarget);
            Assert.AreEqual(executionTarget.TargetId, targetId);
            Assert.AreEqual(executionTarget.PackageName, "Microsoft.Quantum.Providers.Honeywell");

            targetId = "qci.target.name.qpu";
            executionTarget = AzureExecutionTarget.Create(targetId);
            Assert.IsNotNull(executionTarget);
            Assert.AreEqual(executionTarget.TargetId, targetId);
            Assert.AreEqual(executionTarget.PackageName, "Microsoft.Quantum.Providers.QCI");
        }
    }
}
