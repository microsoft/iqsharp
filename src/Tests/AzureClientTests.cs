// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Quantum;
using Microsoft.Azure.Quantum.Client.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.AzureClient;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.IQSharp
{
    [TestClass]
    public class AzureClientTests
    {
        private string originalEnvironmentName = string.Empty;

        [TestInitialize]
        public void SetMockEnvironment()
        {
            originalEnvironmentName = Environment.GetEnvironmentVariable(AzureEnvironment.EnvironmentVariableName) ?? string.Empty;
            Environment.SetEnvironmentVariable(AzureEnvironment.EnvironmentVariableName, AzureEnvironmentType.Mock.ToString());
        }

        [TestCleanup]
        public void RestoreEnvironment()
        {
            Environment.SetEnvironmentVariable(AzureEnvironment.EnvironmentVariableName, originalEnvironmentName);
        }

        private T ExpectSuccess<T>(Task<ExecutionResult> task)
        {
            var result = task.GetAwaiter().GetResult();
            Assert.AreEqual(ExecuteStatus.Ok, result.Status);
            Assert.IsInstanceOfType(result.Output, typeof(T));
            return (T)result.Output;
        }

        private void ExpectError(AzureClientError expectedError, Task<ExecutionResult> task)
        {
            var result = task.GetAwaiter().GetResult();
            Assert.AreEqual(ExecuteStatus.Error, result.Status);
            Assert.IsInstanceOfType(result.Output, typeof(AzureClientError));
            Assert.AreEqual(expectedError, (AzureClientError)result.Output);
        }

        private Task<ExecutionResult> ConnectToWorkspaceAsync(
            IAzureClient azureClient,
            string workspaceName = "TEST_WORKSPACE_NAME",
            string locationName = "")
        {
            MockAzureWorkspace.MockJobIds = new string[] { };
            MockAzureWorkspace.MockTargetIds = new string[] { };
            return azureClient.ConnectAsync(
                new MockChannel(),
                "TEST_SUBSCRIPTION_ID",
                "TEST_RESOURCE_GROUP_NAME",
                workspaceName,
                "TEST_CONNECTION_STRING",
                locationName);
        }

        [TestMethod]
        public void TestAzureEnvironment()
        {
            // Production environment
            Environment.SetEnvironmentVariable(AzureEnvironment.EnvironmentVariableName, AzureEnvironmentType.Production.ToString());
            var environment = AzureEnvironment.Create("TEST_SUBSCRIPTION_ID");
            Assert.AreEqual(AzureEnvironmentType.Production, environment.Type);

            // Dogfood environment
            // NB: This used to intentionally throw an exception when mocked,
            //     due to requiring a service call, but that service call has
            //     been moved to ConnectAsync instead of Create.
            Environment.SetEnvironmentVariable(AzureEnvironment.EnvironmentVariableName, AzureEnvironmentType.Dogfood.ToString());
            environment = AzureEnvironment.Create("TEST_SUBSCRIPTION_ID");
            Assert.AreEqual(AzureEnvironmentType.Dogfood, environment.Type);

            // Canary environment
            Environment.SetEnvironmentVariable(AzureEnvironment.EnvironmentVariableName, AzureEnvironmentType.Canary.ToString());
            environment = AzureEnvironment.Create("TEST_SUBSCRIPTION_ID");
            Assert.AreEqual(AzureEnvironmentType.Canary, environment.Type);

            // Mock environment
            Environment.SetEnvironmentVariable(AzureEnvironment.EnvironmentVariableName, AzureEnvironmentType.Mock.ToString());
            environment = AzureEnvironment.Create("TEST_SUBSCRIPTION_ID");
            Assert.AreEqual(AzureEnvironmentType.Mock, environment.Type);
        }

        [TestMethod]
        public void TestAzureExecutionTarget()
        {
            var targetId = "invalidname";
            var executionTarget = AzureExecutionTarget.Create(targetId);
            Assert.IsNull(executionTarget);

            targetId = "ionq.targetId";
            executionTarget = AzureExecutionTarget.Create(targetId);
            Assert.AreEqual(targetId, executionTarget?.TargetId);
            Assert.AreEqual("Microsoft.Quantum.Providers.IonQ", executionTarget?.PackageName);

            targetId = "HonEYWEll.targetId";
            executionTarget = AzureExecutionTarget.Create(targetId);
            Assert.AreEqual(targetId, executionTarget?.TargetId);
            Assert.AreEqual("Microsoft.Quantum.Providers.Honeywell", executionTarget?.PackageName);

            targetId = "qci.target.name.qpu";
            executionTarget = AzureExecutionTarget.Create(targetId);
            Assert.AreEqual(targetId, executionTarget?.TargetId);
            Assert.AreEqual("Microsoft.Quantum.Providers.QCI", executionTarget?.PackageName);
        }

        [TestMethod]
        public void TestJobStatus()
        {
            var services = Startup.CreateServiceProvider("Workspace");
            var azureClient = (AzureClient)services.GetService<IAzureClient>();

            // not connected
            ExpectError(AzureClientError.NotConnected, azureClient.GetJobStatusAsync(new MockChannel(), "JOB_ID_1"));

            // connect
            var targets = ExpectSuccess<IEnumerable<TargetStatus>>(ConnectToWorkspaceAsync(azureClient));
            Assert.IsFalse(targets.Any());

            // set up the mock workspace
            var azureWorkspace = azureClient.ActiveWorkspace as MockAzureWorkspace;
            Assert.IsNotNull(azureWorkspace);
            MockAzureWorkspace.MockJobIds = new string[] { "JOB_ID_1", "JOB_ID_2" };

            // valid job ID
            var job = ExpectSuccess<CloudJob>(azureClient.GetJobStatusAsync(new MockChannel(), "JOB_ID_1"));
            Assert.AreEqual("JOB_ID_1", job.Id);

            // invalid job ID
            ExpectError(AzureClientError.JobNotFound, azureClient.GetJobStatusAsync(new MockChannel(), "JOB_ID_3"));

            // jobs list with no filter
            var jobs = ExpectSuccess<IEnumerable<CloudJob>>(azureClient.GetJobListAsync(new MockChannel(), string.Empty));
            Assert.AreEqual(2, jobs.Count());

            // jobs list with filter
            jobs = ExpectSuccess<IEnumerable<CloudJob>>(azureClient.GetJobListAsync(new MockChannel(), "JOB_ID_1"));
            Assert.AreEqual(1, jobs.Count());
        }

        [TestMethod]
        public void TestManualTargets()
        {
            var services = Startup.CreateServiceProvider("Workspace");
            var azureClient = (AzureClient)services.GetService<IAzureClient>();

            // SetActiveTargetAsync with recognized target ID, but not yet connected
            ExpectError(AzureClientError.NotConnected, azureClient.SetActiveTargetAsync(new MockChannel(), "ionq.simulator"));

            // GetActiveTargetAsync, but not yet connected
            ExpectError(AzureClientError.NotConnected, azureClient.GetActiveTargetAsync(new MockChannel()));

            // connect
            var targets = ExpectSuccess<IEnumerable<TargetStatus>>(ConnectToWorkspaceAsync(azureClient));
            Assert.IsFalse(targets.Any());

            // set up the mock workspace
            var azureWorkspace = azureClient.ActiveWorkspace as MockAzureWorkspace;
            Assert.IsNotNull(azureWorkspace);
            MockAzureWorkspace.MockTargetIds = new string[] { "ionq.simulator", "honeywell.qpu", "unrecognized.target" };

            // get connection status to verify list of targets
            targets = ExpectSuccess<IEnumerable<TargetStatus>>(azureClient.GetConnectionStatusAsync(new MockChannel()));
            Assert.AreEqual(2, targets.Count()); // only 2 valid quantum execution targets

            // GetActiveTargetAsync, but no active target set yet
            ExpectError(AzureClientError.NoTarget, azureClient.GetActiveTargetAsync(new MockChannel()));

            // SetActiveTargetAsync with target ID not valid for quantum execution
            ExpectError(AzureClientError.InvalidTarget, azureClient.SetActiveTargetAsync(new MockChannel(), "unrecognized.target"));

            // SetActiveTargetAsync with valid target ID
            var target = ExpectSuccess<TargetStatus>(azureClient.SetActiveTargetAsync(new MockChannel(), "ionq.simulator"));
            Assert.AreEqual("ionq.simulator", target.Id);

            // GetActiveTargetAsync
            target = ExpectSuccess<TargetStatus>(azureClient.GetActiveTargetAsync(new MockChannel()));
            Assert.AreEqual("ionq.simulator", target.Id);
        }

        [TestMethod]
        public void TestAllTargets()
        {
            var services = Startup.CreateServiceProvider("Workspace");
            var azureClient = (AzureClient)services.GetService<IAzureClient>();

            // connect to mock workspace with all providers
            var targets = ExpectSuccess<IEnumerable<TargetStatus>>(ConnectToWorkspaceAsync(azureClient, MockAzureWorkspace.NameWithMockProviders));
            Assert.AreEqual(Enum.GetNames(typeof(AzureProvider)).Length, targets.Count());

            // set each target, which will load the corresponding package
            foreach (var target in targets)
            {
                var returnedTarget = ExpectSuccess<TargetStatus>(azureClient.SetActiveTargetAsync(new MockChannel(), target.Id));
                Assert.AreEqual(target.Id, returnedTarget.Id);
            }
        }

        [TestMethod]
        public void TestJobSubmission()
        {
            var services = Startup.CreateServiceProvider("Workspace");
            var azureClient = (AzureClient)services.GetService<IAzureClient>();
            var submissionContext = new AzureSubmissionContext();

            // not yet connected
            ExpectError(AzureClientError.NotConnected, azureClient.SubmitJobAsync(new MockChannel(), submissionContext, CancellationToken.None));

            // connect
            var targets = ExpectSuccess<IEnumerable<TargetStatus>>(ConnectToWorkspaceAsync(azureClient));
            Assert.IsFalse(targets.Any());

            // no target yet
            ExpectError(AzureClientError.NoTarget, azureClient.SubmitJobAsync(new MockChannel(), submissionContext, CancellationToken.None));

            // add a target
            var azureWorkspace = azureClient.ActiveWorkspace as MockAzureWorkspace;
            Assert.IsNotNull(azureWorkspace);
            MockAzureWorkspace.MockTargetIds = new string[] { "ionq.simulator" };

            // set the active target
            var target = ExpectSuccess<TargetStatus>(azureClient.SetActiveTargetAsync(new MockChannel(), "ionq.simulator"));
            Assert.AreEqual("ionq.simulator", target.Id);

            // no operation name specified
            ExpectError(AzureClientError.NoOperationName, azureClient.SubmitJobAsync(new MockChannel(), submissionContext, CancellationToken.None));

            // specify an operation name, but have missing parameters
            submissionContext.OperationName = "Tests.qss.HelloAgain";
            ExpectError(AzureClientError.JobSubmissionFailed, azureClient.SubmitJobAsync(new MockChannel(), submissionContext, CancellationToken.None));

            // specify input parameters and verify that the job was submitted
            submissionContext.InputParameters = AbstractMagic.ParseInputParameters("count=3 name=\"testing\"");
            var job = ExpectSuccess<CloudJob>(azureClient.SubmitJobAsync(new MockChannel(), submissionContext, CancellationToken.None));
            var retrievedJob = ExpectSuccess<CloudJob>(azureClient.GetJobStatusAsync(new MockChannel(), job.Id));
            Assert.AreEqual(job.Id, retrievedJob.Id);
        }

        [TestMethod]
        public void TestJobExecution()
        {
            var services = Startup.CreateServiceProvider("Workspace");
            var azureClient = (AzureClient)services.GetService<IAzureClient>();

            // connect
            var targets = ExpectSuccess<IEnumerable<TargetStatus>>(ConnectToWorkspaceAsync(azureClient));
            Assert.IsFalse(targets.Any());

            // add a target
            var azureWorkspace = azureClient.ActiveWorkspace as MockAzureWorkspace;
            Assert.IsNotNull(azureWorkspace);
            MockAzureWorkspace.MockTargetIds = new string[] { "ionq.simulator" };

            // set the active target
            var target = ExpectSuccess<TargetStatus>(azureClient.SetActiveTargetAsync(new MockChannel(), "ionq.simulator"));
            Assert.AreEqual("ionq.simulator", target.Id);

            // execute the job and verify that the results are retrieved successfully
            var submissionContext = new AzureSubmissionContext()
            {
                OperationName = "Tests.qss.HelloAgain",
                InputParameters = AbstractMagic.ParseInputParameters("count=3 name=\"testing\""),
                ExecutionTimeout = 5,
                ExecutionPollingInterval = 1,
            };
            var histogram = ExpectSuccess<Histogram>(azureClient.ExecuteJobAsync(new MockChannel(), submissionContext, CancellationToken.None));
            Assert.IsNotNull(histogram);
        }

        [TestMethod]
        public void TestJobExecutionWithArrayInput()
        {
            var services = Startup.CreateServiceProvider("Workspace");
            var azureClient = (AzureClient)services.GetService<IAzureClient>();

            // connect
            var targets = ExpectSuccess<IEnumerable<TargetStatus>>(ConnectToWorkspaceAsync(azureClient));
            Assert.IsFalse(targets.Any());

            // add a target
            var azureWorkspace = azureClient.ActiveWorkspace as MockAzureWorkspace;
            Assert.IsNotNull(azureWorkspace);
            MockAzureWorkspace.MockTargetIds = new string[] { "ionq.simulator" };

            // set the active target
            var target = ExpectSuccess<TargetStatus>(azureClient.SetActiveTargetAsync(new MockChannel(), "ionq.simulator"));
            Assert.AreEqual("ionq.simulator", target.Id);

            // execute the job and verify that the results are retrieved successfully
            var submissionContext = new AzureSubmissionContext()
            {
                OperationName = "Tests.qss.SayHelloWithArray",
                InputParameters = AbstractMagic.ParseInputParameters("{\"names\": [\"foo\", \"bar\"]}"),
                ExecutionTimeout = 5,
                ExecutionPollingInterval = 1,
            };
            var histogram = ExpectSuccess<Histogram>(azureClient.ExecuteJobAsync(new MockChannel(), submissionContext, CancellationToken.None));
            Assert.IsNotNull(histogram);
        }

        [TestMethod]
        public void TestRuntimeCapabilities()
        {
            var services = Startup.CreateServiceProvider("Workspace.QPRGen1");
            var azureClient = (AzureClient)services.GetService<IAzureClient>();

            // Choose an operation with measurement result comparison, which should
            // fail to compile on QPRGen0 targets but succeed on QPRGen1 targets
            var submissionContext = new AzureSubmissionContext() { OperationName = "Tests.qss.CompareMeasurementResult" };
            ExpectSuccess<IEnumerable<TargetStatus>>(ConnectToWorkspaceAsync(azureClient));

            // Set up workspace with mock providers
            var azureWorkspace = azureClient.ActiveWorkspace as MockAzureWorkspace;
            Assert.IsNotNull(azureWorkspace);
            MockAzureWorkspace.MockTargetIds = new string[] { "ionq.mock", "honeywell.mock" };

            // Verify that IonQ job fails to compile (QPRGen0)
            ExpectSuccess<TargetStatus>(azureClient.SetActiveTargetAsync(new MockChannel(), "ionq.mock"));
            ExpectError(AzureClientError.InvalidEntryPoint, azureClient.SubmitJobAsync(new MockChannel(), submissionContext, CancellationToken.None));

            // Verify that Honeywell job can be successfully submitted (QPRGen1)
            ExpectSuccess<TargetStatus>(azureClient.SetActiveTargetAsync(new MockChannel(), "honeywell.mock"));
            var job = ExpectSuccess<CloudJob>(azureClient.SubmitJobAsync(new MockChannel(), submissionContext, CancellationToken.None));
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public void TestLocations()
        {
            var services = Startup.CreateServiceProvider("Workspace");
            var azureClient = (AzureClient)services.GetService<IAzureClient>();

            // Default location should be westus
            _ = ExpectSuccess<IEnumerable<TargetStatus>>(ConnectToWorkspaceAsync(azureClient));
            Assert.AreEqual("westus", azureClient.ActiveWorkspace?.Location);

            // Locations with whitespace should be converted correctly
            _ = ExpectSuccess<IEnumerable<TargetStatus>>(ConnectToWorkspaceAsync(azureClient, locationName: "Australia Central 2"));
            Assert.AreEqual("australiacentral2", azureClient.ActiveWorkspace?.Location);

            // Locations with invalid hostname characters should fall back to default westus
            _ = ExpectSuccess<IEnumerable<TargetStatus>>(ConnectToWorkspaceAsync(azureClient, locationName: "/test/"));
            Assert.AreEqual("westus", azureClient.ActiveWorkspace?.Location);
        }
    }
}
