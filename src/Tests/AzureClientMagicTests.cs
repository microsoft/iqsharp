// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.AzureClient;
using Microsoft.Azure.Quantum.Authentication;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.QsCompiler;
using System.Diagnostics.CodeAnalysis;

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
        private readonly string subscriptionId = Guid.NewGuid().ToString();
        private readonly string resourceGroupName = "TEST_RESOURCE_GROUP_NAME";
        private readonly string workspaceName = "TEST_WORKSPACE_NAME";
        private readonly string storageAccountConnectionString = "TEST_CONNECTION_STRING";
        private readonly string location = "TEST_LOCATION";
        private readonly string jobId = "TEST_JOB_ID";
        private readonly string operationName = "TEST_OPERATION_NAME";
        private readonly string targetId = "TEST_TARGET_ID";

        private readonly string jobParams = @"{""TEST_KEY1"":""TEST_VAL1"",""TEST_PATH"":""C:\ABC\XYZ""}";
        private readonly string jobParamsKey1 = "TEST_KEY1";
        private readonly string jobParamsVal1 = "TEST_VAL1";
        private readonly string jobParamsKey2 = "TEST_PATH";
        private readonly string jobParamsVal2 = @"C:\ABC\XYZ";

        private readonly string EnvironmentSubscriptionId = "AZURE_QUANTUM_SUBSCRIPTION_ID";
        private readonly string EnvironmentResourceGroup = "AZURE_QUANTUM_WORKSPACE_RG";
        private readonly string EnvironmentLocation = "AZURE_QUANTUM_WORKSPACE_LOCATION";
        private readonly string EnvironmentWorkspaceName = "AZURE_QUANTUM_WORKSPACE_NAME";

        [TestMethod]
        public void TestConnectMagic()
        {
            var azureClient = new MockAzureClient();
            var logger = new UnitTestLogger<ConnectMagic>();
            var config = new ConfigurationSource(skipLoading: true);
            var connectMagic = new ConnectMagic(azureClient, config, logger);

            // no input
            connectMagic.Test(string.Empty);
            Assert.AreEqual(AzureClientAction.GetConnectionStatus, azureClient.LastAction);

            // unrecognized input
            connectMagic.Test($"invalid", ExecuteStatus.Error);

            // valid input with resource ID (and to verify case-insensitivity of resourceId parsing)
            connectMagic.Test($"resourceId=/subscriptions/{subscriptionId}/RESOurceGroups/{resourceGroupName}/providers/Microsoft.Quantum/Workspaces/{workspaceName}");
            Assert.AreEqual(AzureClientAction.Connect, azureClient.LastAction);
            Assert.AreEqual(subscriptionId, azureClient.SubscriptionId);
            Assert.AreEqual(resourceGroupName, azureClient.ResourceGroupName);
            Assert.AreEqual(workspaceName, azureClient.WorkspaceName);
            Assert.AreEqual(string.Empty, azureClient.ConnectionString);
            Assert.AreEqual(string.Empty, azureClient.Location);

            // valid input with implied resource ID key, without surrounding quotes
            connectMagic.Test($"/subscriptions/{subscriptionId}/RESOurceGroups/{resourceGroupName}/providers/Microsoft.Quantum/Workspaces/{workspaceName}");
            Assert.AreEqual(AzureClientAction.Connect, azureClient.LastAction);
            Assert.AreEqual(subscriptionId, azureClient.SubscriptionId);
            Assert.AreEqual(resourceGroupName, azureClient.ResourceGroupName);
            Assert.AreEqual(workspaceName, azureClient.WorkspaceName);
            Assert.AreEqual(string.Empty, azureClient.ConnectionString);
            Assert.AreEqual(string.Empty, azureClient.Location);
            Assert.AreEqual(CredentialType.Default, azureClient.CredentialType);

            // valid input with implied resource ID key, with surrounding quotes
            connectMagic.Test($"\"/subscriptions/{subscriptionId}/RESOurceGroups/{resourceGroupName}/providers/Microsoft.Quantum/Workspaces/{workspaceName}\"");
            Assert.AreEqual(AzureClientAction.Connect, azureClient.LastAction);
            Assert.AreEqual(subscriptionId, azureClient.SubscriptionId);
            Assert.AreEqual(resourceGroupName, azureClient.ResourceGroupName);
            Assert.AreEqual(workspaceName, azureClient.WorkspaceName);
            Assert.AreEqual(string.Empty, azureClient.ConnectionString);
            Assert.AreEqual(string.Empty, azureClient.Location);
            Assert.AreEqual(CredentialType.Default, azureClient.CredentialType);

            // valid input with resource ID and storage account connection string
            connectMagic.Test(
                @$"resourceId=/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Quantum/Workspaces/{workspaceName}
                   storage={storageAccountConnectionString}
                   credential=cli");
            Assert.AreEqual(AzureClientAction.Connect, azureClient.LastAction);
            Assert.AreEqual(subscriptionId, azureClient.SubscriptionId);
            Assert.AreEqual(resourceGroupName, azureClient.ResourceGroupName);
            Assert.AreEqual(workspaceName, azureClient.WorkspaceName);
            Assert.AreEqual(storageAccountConnectionString, azureClient.ConnectionString);
            Assert.AreEqual(string.Empty, azureClient.Location);
            Assert.AreEqual(CredentialType.CLI, azureClient.CredentialType);

            // valid input with individual parameters
            connectMagic.Test(
                @$"subscription={subscriptionId}
                   resourceGroup={resourceGroupName}
                   workspace={workspaceName}
                   storage={storageAccountConnectionString}
                   location={location}
                   credential=interactive");
            Assert.AreEqual(AzureClientAction.Connect, azureClient.LastAction);
            Assert.AreEqual(subscriptionId, azureClient.SubscriptionId);
            Assert.AreEqual(resourceGroupName, azureClient.ResourceGroupName);
            Assert.AreEqual(workspaceName, azureClient.WorkspaceName);
            Assert.AreEqual(location, azureClient.Location);
            Assert.AreEqual(storageAccountConnectionString, azureClient.ConnectionString);
            Assert.AreEqual(CredentialType.Interactive, azureClient.CredentialType);


            // valid input with extra whitespace and quotes
            connectMagic.Test(
                @$"location ={location}
                   subscription   =   {subscriptionId}
                   resourceGroup=  ""{resourceGroupName}""
                   workspace  ={workspaceName}
                   credential=ENVIRONMENT
                   storage = '{storageAccountConnectionString}'");
            Assert.AreEqual(AzureClientAction.Connect, azureClient.LastAction);
            Assert.AreEqual(subscriptionId, azureClient.SubscriptionId);
            Assert.AreEqual(resourceGroupName, azureClient.ResourceGroupName);
            Assert.AreEqual(workspaceName, azureClient.WorkspaceName);
            Assert.AreEqual(storageAccountConnectionString, azureClient.ConnectionString);
            Assert.AreEqual(CredentialType.Environment, azureClient.CredentialType);
            Assert.AreEqual(location, azureClient.Location);

            // refresh parameter, which has been deprecated so has no effect:
            connectMagic.Test(
                @$"refresh subscription={subscriptionId}
                   resourceGroup={resourceGroupName}
                   workspace={workspaceName}
                   storage={storageAccountConnectionString}");
            Assert.AreEqual(AzureClientAction.Connect, azureClient.LastAction);
            Assert.AreEqual(subscriptionId, azureClient.SubscriptionId);
            Assert.AreEqual(resourceGroupName, azureClient.ResourceGroupName);
            Assert.AreEqual(workspaceName, azureClient.WorkspaceName);
            Assert.AreEqual(storageAccountConnectionString, azureClient.ConnectionString);
            Assert.AreEqual(string.Empty, azureClient.Location);
            Assert.AreEqual(CredentialType.Default, azureClient.CredentialType);

            connectMagic.Test($"refresh /subscriptions/{subscriptionId}/RESOurceGroups/{resourceGroupName}/providers/Microsoft.Quantum/Workspaces/{workspaceName}");
            connectMagic.Test($"/subscriptions/{subscriptionId}/RESOurceGroups/{resourceGroupName}/providers/Microsoft.Quantum/Workspaces/{workspaceName} refresh");
            connectMagic.Test($"/subscriptions/{subscriptionId}/RESOurceGroups/{resourceGroupName}/providers/Microsoft.Quantum/Workspaces/{workspaceName}");
        }

        [TestMethod]
        public void TestConnectMagicFromEnvironment()
        {
            var azureClient = new MockAzureClient();
            var logger = new UnitTestLogger<ConnectMagic>();
            var config = new ConfigurationSource(skipLoading: true);
            var connectMagic = new ConnectMagic(azureClient, config, logger);

            // no input
            connectMagic.Test(string.Empty);
            Assert.AreEqual(AzureClientAction.GetConnectionStatus, azureClient.LastAction);

            // Missing environment variables
            connectMagic.Test($"credential=ENVIRONMENT", ExecuteStatus.Error);

            // Pick up environment variables
            System.Environment.SetEnvironmentVariable(EnvironmentSubscriptionId, subscriptionId);
            System.Environment.SetEnvironmentVariable(EnvironmentResourceGroup, resourceGroupName);
            System.Environment.SetEnvironmentVariable(EnvironmentWorkspaceName, workspaceName);
            System.Environment.SetEnvironmentVariable(EnvironmentLocation, location);

            // Temporarily set the environment variables with old prefix, as the renaming
            // has not being applied to the C# client yet
            System.Environment.SetEnvironmentVariable("AZUREQUANTUM_SUBSCRIPTION_ID", subscriptionId);
            System.Environment.SetEnvironmentVariable("AZUREQUANTUM_WORKSPACE_RG", resourceGroupName);
            System.Environment.SetEnvironmentVariable("AZUREQUANTUM_WORKSPACE_NAME", workspaceName);
            System.Environment.SetEnvironmentVariable("AZUREQUANTUM_WORKSPACE_LOCATION", location);

            connectMagic.Test("credential=ENVIRONMENT");
            Assert.AreEqual(AzureClientAction.Connect, azureClient.LastAction);
            Assert.AreEqual(subscriptionId, azureClient.SubscriptionId);
            Assert.AreEqual(resourceGroupName, azureClient.ResourceGroupName);
            Assert.AreEqual(workspaceName, azureClient.WorkspaceName);
            Assert.AreEqual(string.Empty, azureClient.ConnectionString);
            Assert.AreEqual(CredentialType.Environment, azureClient.CredentialType);
            Assert.AreEqual(location, azureClient.Location);

            // Reset env variables:
            System.Environment.SetEnvironmentVariable(EnvironmentSubscriptionId, string.Empty);
            System.Environment.SetEnvironmentVariable(EnvironmentResourceGroup, string.Empty);
            System.Environment.SetEnvironmentVariable(EnvironmentWorkspaceName, string.Empty);
            System.Environment.SetEnvironmentVariable(EnvironmentLocation, string.Empty);
        }

        [TestMethod]
        public void TestStatusMagic()
        {
            // no arguments - should print job status of most recent job
            var azureClient = new MockAzureClient();
            var statusMagic = new StatusMagic(azureClient, new UnitTestLogger<StatusMagic>());
            statusMagic.Test(string.Empty);
            Assert.AreEqual(AzureClientAction.GetJobStatus, azureClient.LastAction);

            // single argument - should print job status
            azureClient = new MockAzureClient();
            statusMagic = new StatusMagic(azureClient, new UnitTestLogger<StatusMagic>());
            statusMagic.Test($"{jobId}");
            Assert.AreEqual(AzureClientAction.GetJobStatus, azureClient.LastAction);

            // single argument with quotes - should print job status
            azureClient = new MockAzureClient();
            statusMagic = new StatusMagic(azureClient, new UnitTestLogger<StatusMagic>());
            statusMagic.Test($"\"{jobId}\"");
            Assert.AreEqual(AzureClientAction.GetJobStatus, azureClient.LastAction);
        }

        [TestMethod]
        public void TestSubmitMagic()
        {
            // no arguments
            var azureClient = new MockAzureClient();
            var submitMagic = new SubmitMagic(azureClient, new UnitTestLogger<SubmitMagic>());
            submitMagic.Test(string.Empty);
            Assert.AreEqual(AzureClientAction.SubmitJob, azureClient.LastAction);

            // single argument
            submitMagic.Test($"{operationName}");
            Assert.AreEqual(AzureClientAction.SubmitJob, azureClient.LastAction);
            Assert.IsTrue(azureClient.SubmittedJobs.Contains(operationName));

            // jobParams argument
            Assert.IsTrue(azureClient.JobParams.IsEmpty);
            submitMagic.Test($"{operationName} jobParams={jobParams}");
            Assert.IsTrue(azureClient.JobParams.TryGetValue(jobParamsKey1, out string value1));
            Assert.AreEqual(value1, jobParamsVal1);
            Assert.IsTrue(azureClient.JobParams.TryGetValue(jobParamsKey2, out string value2));
            Assert.AreEqual(value2, jobParamsVal2);
        }

        [TestMethod]
        public void TestExecuteMagic()
        {
            // no arguments
            var azureClient = new MockAzureClient();
            var logger = new UnitTestLogger<ExecuteMagic>();
            var executeMagic = new ExecuteMagic(azureClient, logger);
            executeMagic.Test(string.Empty);
            Assert.AreEqual(AzureClientAction.ExecuteJob, azureClient.LastAction);

            // single argument
            executeMagic.Test($"{operationName}");
            Assert.AreEqual(AzureClientAction.ExecuteJob, azureClient.LastAction);
            Assert.IsTrue(azureClient.ExecutedJobs.Contains(operationName));

            // jobParams argument
            Assert.IsTrue(azureClient.JobParams.IsEmpty);
            executeMagic.Test($"{operationName} jobParams={jobParams}");
            Assert.IsTrue(azureClient.JobParams.TryGetValue(jobParamsKey1, out string value1));
            Assert.AreEqual(value1, jobParamsVal1);
            Assert.IsTrue(azureClient.JobParams.TryGetValue(jobParamsKey2, out string value2));
            Assert.AreEqual(value2, jobParamsVal2);
        }

        [TestMethod]
        public void TestOutputMagic()
        {
            // no arguments - should print job result of most recent job
            var azureClient = new MockAzureClient();
            var outputMagic = new OutputMagic(azureClient, new UnitTestLogger<OutputMagic>());
            outputMagic.Test(string.Empty);
            Assert.AreEqual(AzureClientAction.GetJobResult, azureClient.LastAction);

            // single argument - should print job result
            azureClient = new MockAzureClient();
            outputMagic = new OutputMagic(azureClient, new UnitTestLogger<OutputMagic>());
            outputMagic.Test($"{jobId}");
            Assert.AreEqual(AzureClientAction.GetJobResult, azureClient.LastAction);

            // single argument with quotes - should print job result
            azureClient = new MockAzureClient();
            outputMagic = new OutputMagic(azureClient, new UnitTestLogger<OutputMagic>());
            outputMagic.Test($"'{jobId}'");
            Assert.AreEqual(AzureClientAction.GetJobResult, azureClient.LastAction);
        }

        [TestMethod]
        public void TestJobsMagic()
        {
            // no arguments - should print job status of all jobs
            var azureClient = new MockAzureClient();
            var jobsMagic = new JobsMagic(azureClient, new UnitTestLogger<JobsMagic>());
            jobsMagic.Test(string.Empty);
            Assert.AreEqual(AzureClientAction.GetJobList, azureClient.LastAction);

            // with default argument - should still print job status
            azureClient = new MockAzureClient();
            jobsMagic = new JobsMagic(azureClient, new UnitTestLogger<JobsMagic>());
            jobsMagic.Test($"{jobId}");
            Assert.AreEqual(AzureClientAction.GetJobList, azureClient.LastAction);

            // with default and count arguments - should still print job status
            azureClient = new MockAzureClient();
            jobsMagic = new JobsMagic(azureClient, new UnitTestLogger<JobsMagic>());
            jobsMagic.Test($"{jobId} count=1");
            Assert.AreEqual(AzureClientAction.GetJobList, azureClient.LastAction);

            // only with count argument - should still print job status
            azureClient = new MockAzureClient();
            jobsMagic = new JobsMagic(azureClient, new UnitTestLogger<JobsMagic>());
            jobsMagic.Test($"count=1");
            Assert.AreEqual(AzureClientAction.GetJobList, azureClient.LastAction);
        }

        [TestMethod]
        public void TestTargetMagic()
        {
            // single argument - should set active target
            var azureClient = new MockAzureClient();
            var targetMagic = new TargetMagic(azureClient, new UnitTestLogger<TargetMagic>());
            targetMagic.Test(targetId);
            Assert.AreEqual(AzureClientAction.SetActiveTarget, azureClient.LastAction);

            // single argument with quotes - should set active target
            targetMagic = new TargetMagic(azureClient, new UnitTestLogger<TargetMagic>());
            targetMagic.Test($"\"{targetId}\"");
            Assert.AreEqual(AzureClientAction.SetActiveTarget, azureClient.LastAction);

            // no arguments - should print active target
            azureClient = new MockAzureClient();
            targetMagic = new TargetMagic(azureClient, new UnitTestLogger<TargetMagic>());
            targetMagic.Test(string.Empty);
            Assert.AreEqual(AzureClientAction.GetActiveTarget, azureClient.LastAction);
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
        GetQuotaList,
    }

    public class MockAzureClient : IAzureClient
    {
        public Microsoft.Azure.Quantum.IWorkspace? ActiveWorkspace => null;
        internal AzureClientAction LastAction = AzureClientAction.None;
        internal string SubscriptionId = string.Empty;
        internal string ResourceGroupName = string.Empty;
        internal string WorkspaceName = string.Empty;
        internal string ConnectionString = string.Empty;
        internal string Location = string.Empty;
        internal CredentialType CredentialType = CredentialType.Default;
        internal List<string> SubmittedJobs = new List<string>();
        internal List<string> ExecutedJobs = new List<string>();
        internal ImmutableDictionary<string, string> JobParams = ImmutableDictionary<string, string>.Empty;

        string? IAzureClient.ActiveTargetId => ActiveTarget?.TargetId;
        public AzureExecutionTarget? ActiveTarget { get; private set; } = AzureExecutionTarget.Create("mock.mock");
        public TargetCapability TargetCapability { get; private set; } = TargetCapabilityModule.Top;

        public event EventHandler<ConnectToWorkspaceEventArgs>? ConnectToWorkspace;

        public bool TrySetTargetCapability(IChannel? channel, string name, [NotNullWhen(true)] out TargetCapability? capability)
        {
            TargetCapability = TargetCapabilityModule.FromName(name).Value;
            capability = TargetCapability;
            return true;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<ExecutionResult> SetActiveTargetAsync(IChannel channel, string targetId, CancellationToken? token)
        {
            LastAction = AzureClientAction.SetActiveTarget;
            ActiveTarget = MockAzureExecutionTarget.CreateMock(targetId);
            TargetCapability = ActiveTarget?.MaximumCapability;
            return ExecuteStatus.Ok.ToExecutionResult();
        }

        public void ClearActiveTarget()
        {
            ActiveTarget = null;
            TargetCapability = null;
        }

        public async Task<ExecutionResult> GetActiveTargetAsync(IChannel channel, CancellationToken? token)
        {
            LastAction = AzureClientAction.GetActiveTarget;
            return (ActiveTarget?.TargetId).ToExecutionResult();
        }

        public async Task<ExecutionResult> SubmitJobAsync(IChannel channel, AzureSubmissionContext submissionContext, CancellationToken? token)
        {
            LastAction = AzureClientAction.SubmitJob;
            SubmittedJobs.Add(submissionContext.OperationName);
            JobParams = submissionContext.InputParams;
            return ExecuteStatus.Ok.ToExecutionResult();
        }

        public async Task<ExecutionResult> ExecuteJobAsync(IChannel channel, AzureSubmissionContext submissionContext, CancellationToken? token)
        {
            LastAction = AzureClientAction.ExecuteJob;
            ExecutedJobs.Add(submissionContext.OperationName);
            JobParams = submissionContext.InputParams;
            return ExecuteStatus.Ok.ToExecutionResult();
        }

        public async Task<ExecutionResult> ConnectAsync(IChannel channel,
            string subscriptionId,
            string resourceGroupName,
            string workspaceName,
            string storageAccountConnectionString,
            string location,
            CredentialType credentialType,
            CancellationToken? cancellationToken = null)
        {
            LastAction = AzureClientAction.Connect;
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            WorkspaceName = workspaceName;
            ConnectionString = storageAccountConnectionString;
            Location = location;
            CredentialType = credentialType;

            ConnectToWorkspace?.Invoke(this, new ConnectToWorkspaceEventArgs(ExecuteStatus.Ok, null, location, storageAccountConnectionString != null, credentialType, TimeSpan.FromMilliseconds(1)));
            return ExecuteStatus.Ok.ToExecutionResult();
        }

        public async Task<ExecutionResult> GetConnectionStatusAsync(IChannel channel, CancellationToken? token)
        {
            LastAction = AzureClientAction.GetConnectionStatus;
            return ExecuteStatus.Ok.ToExecutionResult();
        }

        public async Task<ExecutionResult> GetJobListAsync(IChannel channel, string filter, int? count = default, CancellationToken? token = default)
        {
            LastAction = AzureClientAction.GetJobList;
            return ExecuteStatus.Ok.ToExecutionResult();
        }

        public async Task<ExecutionResult> GetJobStatusAsync(IChannel channel, string jobId, CancellationToken? token)
        {
            LastAction = AzureClientAction.GetJobStatus;
            return ExecuteStatus.Ok.ToExecutionResult();
        }

        public async Task<ExecutionResult> GetJobResultAsync(IChannel channel, string jobId, CancellationToken? token)
        {
            LastAction = AzureClientAction.GetJobResult;
            return ExecuteStatus.Ok.ToExecutionResult();
        }

        public async Task<ExecutionResult> GetQuotaListAsync(IChannel channel, CancellationToken? token)
        {
            LastAction = AzureClientAction.GetQuotaList;
            return ExecuteStatus.Ok.ToExecutionResult();
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
