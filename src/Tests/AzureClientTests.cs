﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Quantum;
using Microsoft.Azure.Quantum.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.AzureClient;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.IQSharp
{
    [TestClass]
    public class AzureClientTests
    {
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
            // Reset the global set of jobs and providers everytime we connect to a new workspace:
            MockAzureWorkspace.MockJobIds = new string[] { };
            MockAzureWorkspace.MockProviders = new HashSet<string>();

            return azureClient.ConnectAsync(
                new MockChannel(),
                "TEST_SUBSCRIPTION_ID",
                "TEST_RESOURCE_GROUP_NAME",
                workspaceName,
                "TEST_CONNECTION_STRING",
                locationName,
                CredentialType.Environment);
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
            var targets = ExpectSuccess<IEnumerable<TargetStatusInfo>>(ConnectToWorkspaceAsync(azureClient));
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
            var targets = ExpectSuccess<IEnumerable<TargetStatusInfo>>(ConnectToWorkspaceAsync(azureClient));
            Assert.IsFalse(targets.Any());

            // set up the mock workspace
            var azureWorkspace = azureClient.ActiveWorkspace as MockAzureWorkspace;
            Assert.IsNotNull(azureWorkspace);
            azureWorkspace?.AddProviders("ionq", "honeywell", "unrecognized");

            // get connection status to verify list of targets
            targets = ExpectSuccess<IEnumerable<TargetStatusInfo>>(azureClient.GetConnectionStatusAsync(new MockChannel()));
            Assert.AreEqual(4, targets.Count()); // only 2 valid quantum execution targets

            // GetActiveTargetAsync, but no active target set yet
            ExpectError(AzureClientError.NoTarget, azureClient.GetActiveTargetAsync(new MockChannel()));

            // SetActiveTargetAsync with target ID not valid for quantum execution
            ExpectError(AzureClientError.InvalidTarget, azureClient.SetActiveTargetAsync(new MockChannel(), "unrecognized.simulator"));

            // SetActiveTargetAsync with valid target ID
            var target = ExpectSuccess<TargetStatusInfo>(azureClient.SetActiveTargetAsync(new MockChannel(), "ionq.simulator"));
            Assert.AreEqual("ionq.simulator", target.TargetId);

            // GetActiveTargetAsync
            target = ExpectSuccess<TargetStatusInfo>(azureClient.GetActiveTargetAsync(new MockChannel()));
            Assert.AreEqual("ionq.simulator", target.TargetId);
        }

        [TestMethod]
        public void TestAllTargets()
        {
            var services = Startup.CreateServiceProvider("Workspace");
            var azureClient = (AzureClient)services.GetService<IAzureClient>();

            // connect to mock workspace with all providers
            var targets = ExpectSuccess<IEnumerable<TargetStatusInfo>>(ConnectToWorkspaceAsync(azureClient, MockAzureWorkspace.NameWithMockProviders));
            // 2 targets per provider: mock and simulator.
            Assert.AreEqual(2 * Enum.GetNames(typeof(AzureProvider)).Length, targets.Count());

            // set each target, which will load the corresponding package
            foreach (var target in targets)
            {
                var returnedTarget = ExpectSuccess<TargetStatusInfo>(azureClient.SetActiveTargetAsync(new MockChannel(), target.TargetId ?? string.Empty));
                Assert.AreEqual(target.TargetId, returnedTarget.TargetId);
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
            var targets = ExpectSuccess<IEnumerable<TargetStatusInfo>>(ConnectToWorkspaceAsync(azureClient));
            Assert.IsFalse(targets.Any());

            // no target yet
            ExpectError(AzureClientError.NoTarget, azureClient.SubmitJobAsync(new MockChannel(), submissionContext, CancellationToken.None));

            // add a target
            var azureWorkspace = azureClient.ActiveWorkspace as MockAzureWorkspace;
            Assert.IsNotNull(azureWorkspace);
            azureWorkspace?.AddProviders("ionq");

            // set the active target
            var target = ExpectSuccess<TargetStatusInfo>(azureClient.SetActiveTargetAsync(new MockChannel(), "ionq.simulator"));
            Assert.AreEqual("ionq.simulator", target.TargetId);

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
            var targets = ExpectSuccess<IEnumerable<TargetStatusInfo>>(ConnectToWorkspaceAsync(azureClient));
            Assert.IsFalse(targets.Any());

            // add a target
            var azureWorkspace = azureClient.ActiveWorkspace as MockAzureWorkspace;
            Assert.IsNotNull(azureWorkspace);
            azureWorkspace?.AddProviders("ionq");

            // set the active target
            var target = ExpectSuccess<TargetStatusInfo>(azureClient.SetActiveTargetAsync(new MockChannel(), "ionq.simulator"));
            Assert.AreEqual("ionq.simulator", target.TargetId);

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
            var targets = ExpectSuccess<IEnumerable<TargetStatusInfo>>(ConnectToWorkspaceAsync(azureClient));
            Assert.IsFalse(targets.Any());

            // add a target
            var azureWorkspace = azureClient.ActiveWorkspace as MockAzureWorkspace;
            Assert.IsNotNull(azureWorkspace);
            azureWorkspace?.AddProviders("ionq");

            // set the active target
            var target = ExpectSuccess<TargetStatusInfo>(azureClient.SetActiveTargetAsync(new MockChannel(), "ionq.simulator"));
            Assert.AreEqual("ionq.simulator", target.TargetId);

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
            ExpectSuccess<IEnumerable<TargetStatusInfo>>(ConnectToWorkspaceAsync(azureClient));

            // Set up workspace with mock providers
            var azureWorkspace = azureClient.ActiveWorkspace as MockAzureWorkspace;
            Assert.IsNotNull(azureWorkspace);
            azureWorkspace?.AddProviders("ionq", "honeywell");

            // Verify that IonQ job fails to compile (QPRGen0)
            ExpectSuccess<TargetStatusInfo>(azureClient.SetActiveTargetAsync(new MockChannel(), "ionq.mock"));
            ExpectError(AzureClientError.InvalidEntryPoint, azureClient.SubmitJobAsync(new MockChannel(), submissionContext, CancellationToken.None));

            // Verify that Honeywell job can be successfully submitted (QPRGen1)
            ExpectSuccess<TargetStatusInfo>(azureClient.SetActiveTargetAsync(new MockChannel(), "honeywell.mock"));
            var job = ExpectSuccess<CloudJob>(azureClient.SubmitJobAsync(new MockChannel(), submissionContext, CancellationToken.None));
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public void TestLocations()
        {
            var services = Startup.CreateServiceProvider("Workspace");
            var azureClient = (AzureClient)services.GetService<IAzureClient>();

            // Default location should be westus
            _ = ExpectSuccess<IEnumerable<TargetStatusInfo>>(ConnectToWorkspaceAsync(azureClient));
            Assert.AreEqual("westus", azureClient.ActiveWorkspace?.Location);

            // Locations with whitespace should be converted correctly
            _ = ExpectSuccess<IEnumerable<TargetStatusInfo>>(ConnectToWorkspaceAsync(azureClient, locationName: "Australia Central 2"));
            Assert.AreEqual("australiacentral2", azureClient.ActiveWorkspace?.Location);

            // Locations with invalid hostname characters should fall back to default westus
            _ = ExpectSuccess<IEnumerable<TargetStatusInfo>>(ConnectToWorkspaceAsync(azureClient, locationName: "/test/"));
            Assert.AreEqual("westus", azureClient.ActiveWorkspace?.Location);
        }

        [TestMethod]
        public void TestConnectedEvent()
        {
            var services = Startup.CreateServiceProvider("Workspace");
            var azureClient = (AzureClient)services.GetService<IAzureClient>();

            ConnectToWorkspaceEventArgs? lastArgs = null;

            // connect
            azureClient.ConnectToWorkspace += (object? sender, ConnectToWorkspaceEventArgs e) =>
            {
                lastArgs = e;
            };

            ExpectError(AzureClientError.WorkspaceNotFound, azureClient.ConnectAsync(
                new MockChannel(),
                "TEST_SUBSCRIPTION_ID",
                "TEST_RESOURCE_GROUP_NAME",
                MockAzureWorkspace.NameForInvalidWorkspace,
                string.Empty,
                "TEST_LOCATION",
                CredentialType.Default));

            Assert.IsNotNull(lastArgs);
            if (lastArgs != null)
            {
                Assert.AreEqual(ExecuteStatus.Error, lastArgs.Status);
                Assert.AreEqual(AzureClientError.WorkspaceNotFound, lastArgs.Error);
                Assert.AreEqual(CredentialType.Default, lastArgs.CredentialType);
                Assert.AreEqual("TEST_LOCATION", lastArgs.Location);
                Assert.AreEqual(false, lastArgs.UseCustomStorage);
            }

            lastArgs = null;
            _ = ExpectSuccess<IEnumerable<TargetStatusInfo>>(ConnectToWorkspaceAsync(azureClient, locationName: "TEST_LOCATION"));
            Assert.IsNotNull(lastArgs);
            if (lastArgs != null)
            {
                Assert.AreEqual(ExecuteStatus.Ok, lastArgs.Status);
                Assert.AreEqual(null, lastArgs.Error);
                Assert.AreEqual(CredentialType.Environment, lastArgs.CredentialType);
                Assert.AreEqual("TEST_LOCATION", lastArgs.Location);
                Assert.AreEqual(true, lastArgs.UseCustomStorage);
            }
        }
    }
}
