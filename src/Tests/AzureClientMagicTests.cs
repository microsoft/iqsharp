// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.AzureClient;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.IQSharp
{
    public static class AzureClientMagicTestExtensions
    {
        public static void Test(this MagicSymbol magic, string input, ExecuteStatus expected = ExecuteStatus.Ok)
        {
            var result = magic.Execute(input, new MockChannel()).GetAwaiter().GetResult();
            Assert.IsTrue(result.Status == expected);
        }
    }

    [TestClass]
    public class AzureClientMagicTests
    {
        private readonly string subscriptionId = "TEST_SUBSCRIPTION_ID";
        private readonly string resourceGroupName = "TEST_RESOURCE_GROUP_NAME";
        private readonly string workspaceName = "TEST_WORKSPACE_NAME";
        private readonly string storageAccountConnectionString = "TEST_CONNECTION_STRING";
        private readonly string jobId = "TEST_JOB_ID";
        private readonly string operationName = "TEST_OPERATION_NAME";
        private readonly string targetId = "TEST_TARGET_ID";

        [TestMethod]
        public void TestConnectMagic()
        {
            var azureClient = new MockAzureClient();
            var connectMagic = new ConnectMagic(azureClient);

            // unrecognized input
            connectMagic.Test($"invalid");
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.GetConnectionStatus);

            // valid input
            connectMagic.Test(
                @$"subscriptionId={subscriptionId}
                   resourceGroupName={resourceGroupName}
                   workspaceName={workspaceName}
                   storageAccountConnectionString={storageAccountConnectionString}");
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.Connect);
            Assert.IsFalse(azureClient.RefreshCredentials);
            Assert.AreEqual(azureClient.ConnectionString, storageAccountConnectionString);

            // valid input with forced login
            connectMagic.Test(
                @$"refresh subscriptionId={subscriptionId}
                   resourceGroupName={resourceGroupName}
                   workspaceName={workspaceName}
                   storageAccountConnectionString={storageAccountConnectionString}");

            Assert.IsTrue(azureClient.RefreshCredentials);
        }

        [TestMethod]
        public void TestStatusMagic()
        {
            // no arguments - should print job status of most recent job
            var azureClient = new MockAzureClient();
            var statusMagic = new StatusMagic(azureClient);
            statusMagic.Test(string.Empty);
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.GetJobStatus);

            // single argument - should print job status
            azureClient = new MockAzureClient();
            statusMagic = new StatusMagic(azureClient);
            statusMagic.Test($"{jobId}");
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.GetJobStatus);
        }

        [TestMethod]
        public void TestSubmitMagic()
        {
            // no arguments
            var azureClient = new MockAzureClient();
            var submitMagic = new SubmitMagic(azureClient);
            submitMagic.Test(string.Empty);
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.SubmitJob);

            // single argument
            submitMagic.Test($"{operationName}");
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.SubmitJob);
            Assert.IsTrue(azureClient.SubmittedJobs.Contains(operationName));
        }

        [TestMethod]
        public void TestExecuteMagic()
        {
            // no arguments
            var azureClient = new MockAzureClient();
            var executeMagic = new ExecuteMagic(azureClient);
            executeMagic.Test(string.Empty);
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.ExecuteJob);

            // single argument
            executeMagic.Test($"{operationName}");
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.ExecuteJob);
            Assert.IsTrue(azureClient.ExecutedJobs.Contains(operationName));
        }

        [TestMethod]
        public void TestOutputMagic()
        {
            // no arguments - should print job result of most recent job
            var azureClient = new MockAzureClient();
            var outputMagic = new OutputMagic(azureClient);
            outputMagic.Test(string.Empty);
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.GetJobResult);

            // single argument - should print job status
            azureClient = new MockAzureClient();
            outputMagic = new OutputMagic(azureClient);
            outputMagic.Test($"{jobId}");
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.GetJobResult);
        }

        [TestMethod]
        public void TestJobsMagic()
        {
            // no arguments - should print job status of all jobs
            var azureClient = new MockAzureClient();
            var jobsMagic = new JobsMagic(azureClient);
            jobsMagic.Test(string.Empty);
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.GetJobList);
        }

        [TestMethod]
        public void TestTargetMagic()
        {
            // single argument - should set active target
            var azureClient = new MockAzureClient();
            var targetMagic = new TargetMagic(azureClient);
            targetMagic.Test(targetId);
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.SetActiveTarget);

            // no arguments - should print active target
            azureClient = new MockAzureClient();
            targetMagic = new TargetMagic(azureClient);
            targetMagic.Test(string.Empty);
            Assert.AreEqual(azureClient.LastAction, AzureClientAction.GetActiveTarget);
        }
    }

    internal enum AzureClientAction
    {
        None,
        Connect,
        SetActiveTarget,
        GetActiveTarget,
        SubmitJob,
        ExecuteJob,
        GetConnectionStatus,
        GetJobList,
        GetJobStatus,
        GetJobResult,
    }

    public class MockAzureClient : IAzureClient
    {
        internal AzureClientAction LastAction = AzureClientAction.None;
        internal string ConnectionString = string.Empty;
        internal bool RefreshCredentials = false;
        internal string ActiveTargetId = string.Empty;
        internal List<string> SubmittedJobs = new List<string>();
        internal List<string> ExecutedJobs = new List<string>();

        public async Task<ExecutionResult> SetActiveTargetAsync(IChannel channel, string targetId)
        {
            LastAction = AzureClientAction.SetActiveTarget;
            ActiveTargetId = targetId;
            return ExecuteStatus.Ok.ToExecutionResult();
        }
        public async Task<ExecutionResult> GetActiveTargetAsync(IChannel channel)
        {
            LastAction = AzureClientAction.GetActiveTarget;
            return ActiveTargetId.ToExecutionResult();
        }

        public async Task<ExecutionResult> SubmitJobAsync(IChannel channel, string operationName, Dictionary<string, string> inputParameters)
        {
            LastAction = AzureClientAction.SubmitJob;
            SubmittedJobs.Add(operationName);
            return ExecuteStatus.Ok.ToExecutionResult();
        }

        public async Task<ExecutionResult> ExecuteJobAsync(IChannel channel, string operationName, Dictionary<string, string> inputParameters)
        {
            LastAction = AzureClientAction.ExecuteJob;
            ExecutedJobs.Add(operationName);
            return ExecuteStatus.Ok.ToExecutionResult();
        }

        public async Task<ExecutionResult> ConnectAsync(IChannel channel, string subscriptionId, string resourceGroupName, string workspaceName, string storageAccountConnectionString, bool refreshCredentials)
        {
            LastAction = AzureClientAction.Connect;
            ConnectionString = storageAccountConnectionString;
            RefreshCredentials = refreshCredentials;
            return ExecuteStatus.Ok.ToExecutionResult();
        }

        public async Task<ExecutionResult> GetConnectionStatusAsync(IChannel channel)
        {
            LastAction = AzureClientAction.GetConnectionStatus;
            return ExecuteStatus.Ok.ToExecutionResult();
        }

        public async Task<ExecutionResult> GetJobListAsync(IChannel channel)
        {
            LastAction = AzureClientAction.GetJobList;
            return ExecuteStatus.Ok.ToExecutionResult();
        }

        public async Task<ExecutionResult> GetJobStatusAsync(IChannel channel, string jobId)
        {
            LastAction = AzureClientAction.GetJobStatus;
            return ExecuteStatus.Ok.ToExecutionResult();
        }

        public async Task<ExecutionResult> GetJobResultAsync(IChannel channel, string jobId)
        {
            LastAction = AzureClientAction.GetJobResult;
            return ExecuteStatus.Ok.ToExecutionResult();
        }
    }
}
