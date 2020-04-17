﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Quantum.IQSharp.AzureClient;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Jupyter.Core;
using System.Threading.Tasks;
using Microsoft.Quantum.IQSharp;

namespace Tests.IQSharp
{
    public static class AzureClientTestExtensions
    {
        public static void Test(this MagicSymbol magic, string input, ExecuteStatus expected = ExecuteStatus.Ok)
        {
            var result = magic.Execute(input, new MockChannel()).GetAwaiter().GetResult();
            Assert.IsTrue(result.Status == expected);
        }
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
        public void TestConnectMagic()
        {
            var azureClient = new MockAzureClient();
            var connectMagic = new ConnectMagic(azureClient);

            // unrecognized input
            connectMagic.Test($"invalid");
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.PrintConnectionStatus);

            // valid input
            connectMagic.Test(
                @$"subscriptionId={subscriptionId}
                   resourceGroupName={resourceGroupName}
                   workspaceName={workspaceName}
                   storageAccountConnectionString={storageAccountConnectionString}");
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.Connect);
            Assert.IsFalse(azureClient.ForceLogin);
            Assert.AreEqual(azureClient.ConnectionString, storageAccountConnectionString);

            // valid input with forced login
            connectMagic.Test(
                @$"login
                   subscriptionId={subscriptionId}
                   resourceGroupName={resourceGroupName}
                   workspaceName={workspaceName}
                   storageAccountConnectionString={storageAccountConnectionString}");

            Assert.IsTrue(azureClient.ForceLogin);
        }

        [TestMethod]
        public void TestStatusMagic()
        {
            // no arguments - should print job list
            var azureClient = new MockAzureClient();
            var statusMagic = new StatusMagic(azureClient);
            statusMagic.Test(string.Empty);
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.PrintJobList);

            // single argument - should print job status
            azureClient = new MockAzureClient();
            statusMagic = new StatusMagic(azureClient);
            statusMagic.Test($"{jobId}");
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.PrintJobStatus);
        }

        [TestMethod]
        public void TestSubmitMagic()
        {
            // no arguments
            var azureClient = new MockAzureClient();
            var operationResolver = new MockOperationResolver();
            var submitMagic = new SubmitMagic(operationResolver, azureClient);
            submitMagic.Test(string.Empty);
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.SubmitJob);

            // single argument
            submitMagic.Test($"{operationName}");
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.SubmitJob);
            Assert.IsTrue(azureClient.SubmittedJobs.Contains(operationName));
        }

        [TestMethod]
        public void TestTargetMagic()
        {
            // no arguments - should print target list
            var azureClient = new MockAzureClient();
            var targetMagic = new TargetMagic(azureClient);
            targetMagic.Test(string.Empty);
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.PrintTargetList);

            // single argument - should set active target
            azureClient = new MockAzureClient();
            targetMagic = new TargetMagic(azureClient);
            targetMagic.Test(targetName);
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.SetActiveTarget);
        }
    }

    internal enum AzureClientAction
    {
        None,
        Connect,
        SetActiveTarget,
        SubmitJob,
        PrintActiveTarget,
        PrintConnectionStatus,
        PrintJobList,
        PrintJobStatus,
        PrintTargetList,
    }

    public class MockAzureClient : IAzureClient
    {
        internal AzureClientAction LastAction = AzureClientAction.None;
        internal string ConnectionString = string.Empty;
        internal bool ForceLogin = false;
        internal string ActiveTargetName = string.Empty;
        internal List<string> SubmittedJobs = new List<string>();

        public async Task<AzureClientError> SetActiveTargetAsync(IChannel channel, string targetName)
        {
            LastAction = AzureClientAction.SetActiveTarget;
            ActiveTargetName = targetName;
            return AzureClientError.Success;
        }

        public async Task<AzureClientError> SubmitJobAsync(IChannel channel, IOperationResolver operationResolver, string operationName)
        {
            LastAction = AzureClientAction.SubmitJob;
            SubmittedJobs.Add(operationName);
            return AzureClientError.Success;
        }

        public async Task<AzureClientError> ConnectAsync(IChannel channel, string subscriptionId, string resourceGroupName, string workspaceName, string storageAccountConnectionString, bool forceLogin)
        {
            LastAction = AzureClientAction.Connect;
            ConnectionString = storageAccountConnectionString;
            ForceLogin = forceLogin;
            return AzureClientError.Success;
        }

        public async Task<AzureClientError> PrintActiveTargetAsync(IChannel channel)
        {
            LastAction = AzureClientAction.PrintActiveTarget;
            return AzureClientError.Success;
        }

        public async Task<AzureClientError> PrintConnectionStatusAsync(IChannel channel)
        {
            LastAction = AzureClientAction.PrintConnectionStatus;
            return AzureClientError.Success;
        }

        public async Task<AzureClientError> PrintJobListAsync(IChannel channel)
        {
            LastAction = AzureClientAction.PrintJobList;
            return AzureClientError.Success;
        }

        public async Task<AzureClientError> PrintJobStatusAsync(IChannel channel, string jobId)
        {
            LastAction = AzureClientAction.PrintJobStatus;
            return AzureClientError.Success;
        }

        public async Task<AzureClientError> PrintTargetListAsync(IChannel channel)
        {
            LastAction = AzureClientAction.PrintTargetList;
            return AzureClientError.Success;
        }
    }
}
