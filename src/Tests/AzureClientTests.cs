// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Azure.Quantum;
using Microsoft.Azure.Quantum.Client.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.AzureClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Resolver;

namespace Tests.IQSharp
{
    [TestClass]
    public class AzureClientTests
    {
        private string originalEnvironmentName = string.Empty;

        [TestInitialize]
        public void SetMockEnvironment()
        {
            originalEnvironmentName = Environment.GetEnvironmentVariable(AzureEnvironment.EnvironmentVariableName);
            Environment.SetEnvironmentVariable(AzureEnvironment.EnvironmentVariableName, AzureEnvironmentType.Mock.ToString());
        }

        [TestCleanup]
        public void RestoreEnvironment()
        {
            Environment.SetEnvironmentVariable(AzureEnvironment.EnvironmentVariableName, originalEnvironmentName);
        }

        [TestMethod]
        public void TestAzureEnvironment()
        {
            // Production environment
            Environment.SetEnvironmentVariable(AzureEnvironment.EnvironmentVariableName, AzureEnvironmentType.Production.ToString());
            var environment = AzureEnvironment.Create("TEST_SUBSCRIPTION_ID");
            Assert.AreEqual(environment.Type, AzureEnvironmentType.Production);

            // Dogfood environment cannot be created in test because it requires a service call
            Environment.SetEnvironmentVariable(AzureEnvironment.EnvironmentVariableName, AzureEnvironmentType.Dogfood.ToString());
            Assert.ThrowsException<InvalidOperationException>(() => AzureEnvironment.Create("TEST_SUBSCRIPTION_ID"));

            // Canary environment
            Environment.SetEnvironmentVariable(AzureEnvironment.EnvironmentVariableName, AzureEnvironmentType.Canary.ToString());
            environment = AzureEnvironment.Create("TEST_SUBSCRIPTION_ID");
            Assert.AreEqual(environment.Type, AzureEnvironmentType.Canary);

            // Mock environment
            Environment.SetEnvironmentVariable(AzureEnvironment.EnvironmentVariableName, AzureEnvironmentType.Mock.ToString());
            environment = AzureEnvironment.Create("TEST_SUBSCRIPTION_ID");
            Assert.AreEqual(environment.Type, AzureEnvironmentType.Mock);
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

        [TestMethod]
        public void TestJobStatus()
        {
            var services = Startup.CreateServiceProvider("Workspace");
            var azureClient = services.GetService<IAzureClient>() as AzureClient;

            // connect
            var result = azureClient.ConnectAsync(new MockChannel(), "TEST_SUBSCRIPTION_ID", "TEST_RESOURCE_GROUP_NAME", "TEST_WORKSPACE_NAME", "TEST_CONNECTION_STRING").GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Ok);

            // set up the mock workspace
            var azureWorkspace = azureClient.ActiveWorkspace as MockAzureWorkspace;
            azureWorkspace.AddMockJobs("JOB_ID_1", "JOB_ID_2");

            // valid job ID
            result = azureClient.GetJobStatusAsync(new MockChannel(), "JOB_ID_1").GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Ok);
            var job = result.Output as CloudJob;
            Assert.IsNotNull(job);
            Assert.AreEqual(job.Id, "JOB_ID_1");

            // invalid job ID
            result = azureClient.GetJobStatusAsync(new MockChannel(), "JOB_ID_3").GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Error);

            // jobs list
            result = azureClient.GetJobListAsync(new MockChannel()).GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Ok);
            var jobs = result.Output as IEnumerable<CloudJob>;
            Assert.IsNotNull(jobs);
            Assert.AreEqual(jobs.Count(), 2);
        }

        [TestMethod]
        public void TestTargets()
        {
            var services = Startup.CreateServiceProvider("Workspace");
            var azureClient = services.GetService<IAzureClient>() as AzureClient;

            // SetActiveTargetAsync with recognized target ID, but not yet connected
            var result = azureClient.SetActiveTargetAsync(new MockChannel(), "ionq.simulator").GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Error);

            // SetActiveTargetAsync with unrecognized target ID
            result = azureClient.SetActiveTargetAsync(new MockChannel(), "unrecognized.target").GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Error);

            // GetActiveTargetAsync, but not yet connected
            result = azureClient.GetActiveTargetAsync(new MockChannel()).GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Error);

            // connect
            result = azureClient.ConnectAsync(new MockChannel(), "TEST_SUBSCRIPTION_ID", "TEST_RESOURCE_GROUP_NAME", "TEST_WORKSPACE_NAME", "TEST_CONNECTION_STRING").GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Ok);

            // set up the mock workspace
            var azureWorkspace = azureClient.ActiveWorkspace as MockAzureWorkspace;
            azureWorkspace.AddMockTargets("ionq.simulator", "honeywell.qpu", "unrecognized.target");

            // get connection status to verify list of targets
            result = azureClient.GetConnectionStatusAsync(new MockChannel()).GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Ok);
            var targets = result.Output as IEnumerable<TargetStatus>;
            Assert.AreEqual(targets.Count(), 2); // only 2 valid quantum execution targets

            // SetActiveTargetAsync with target ID not valid for quantum execution
            result = azureClient.SetActiveTargetAsync(new MockChannel(), "unrecognized.target").GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Error);

            // SetActiveTargetAsync with valid target ID
            result = azureClient.SetActiveTargetAsync(new MockChannel(), "ionq.simulator").GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Ok);

            // GetActiveTargetAsync
            result = azureClient.GetActiveTargetAsync(new MockChannel()).GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Ok);
            var targetId = result.Output as string;
            Assert.AreEqual(targetId, "ionq.simulator");
        }

        [TestMethod]
        public void TestJobSubmission()
        {
            var services = Startup.CreateServiceProvider("Workspace");
            var azureClient = services.GetService<IAzureClient>() as AzureClient;
            var submissionContext = new AzureSubmissionContext();

            // not yet connected
            var result = azureClient.SubmitJobAsync(new MockChannel(), submissionContext).GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Error);

            // connect
            result = azureClient.ConnectAsync(new MockChannel(), "TEST_SUBSCRIPTION_ID", "TEST_RESOURCE_GROUP_NAME", "TEST_WORKSPACE_NAME", "TEST_CONNECTION_STRING").GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Ok);

            // no target yet
            result = azureClient.SubmitJobAsync(new MockChannel(), submissionContext).GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Error);

            // add a target
            var azureWorkspace = azureClient.ActiveWorkspace as MockAzureWorkspace;
            azureWorkspace.AddMockTargets("ionq.simulator");
            result = azureClient.SetActiveTargetAsync(new MockChannel(), "ionq.simulator").GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Ok);

            // no operation name specified
            result = azureClient.SubmitJobAsync(new MockChannel(), submissionContext).GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Error);

            // specify an operation name, but have missing parameters
            submissionContext.OperationName = "Tests.qss.HelloAgain";
            result = azureClient.SubmitJobAsync(new MockChannel(), submissionContext).GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Error);

            // specify input parameters and verify that the job was submitted
            submissionContext.InputParameters = new Dictionary<string, string>() { ["count"] = "3", ["name"] = "testing" };
            result = azureClient.SubmitJobAsync(new MockChannel(), submissionContext).GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Ok);
            var job = result.Output as CloudJob;
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public void TestJobExecution()
        {
            var services = Startup.CreateServiceProvider("Workspace");
            var azureClient = services.GetService<IAzureClient>() as AzureClient;

            // connect
            var result = azureClient.ConnectAsync(new MockChannel(), "TEST_SUBSCRIPTION_ID", "TEST_RESOURCE_GROUP_NAME", "TEST_WORKSPACE_NAME", "TEST_CONNECTION_STRING").GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Ok);

            // add a target
            var azureWorkspace = azureClient.ActiveWorkspace as MockAzureWorkspace;
            azureWorkspace.AddMockTargets("ionq.simulator");
            result = azureClient.SetActiveTargetAsync(new MockChannel(), "ionq.simulator").GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Ok);

            // execute the job and verify that the results are retrieved successfully
            var submissionContext = new AzureSubmissionContext()
            {
                OperationName = "Tests.qss.HelloAgain",
                InputParameters = new Dictionary<string, string>() { ["count"] = "3", ["name"] = "testing" },
                ExecutionTimeout = 5,
                ExecutionPollingInterval = 1,
            };
            result = azureClient.ExecuteJobAsync(new MockChannel(), submissionContext).GetAwaiter().GetResult();
            Assert.AreEqual(result.Status, ExecuteStatus.Ok);
            var histogram = result.Output as Histogram;
            Assert.IsNotNull(histogram);
        }
    }
}
