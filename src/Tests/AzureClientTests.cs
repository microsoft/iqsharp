// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Quantum.Jobs.Models;
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
            string locationName = "TEST_LOCATION")
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

            // Check that deprecated targets still work.
            targetId = "HonEYWEll.targetId";
            executionTarget = AzureExecutionTarget.Create(targetId);
            Assert.AreEqual(targetId, executionTarget?.TargetId);
            Assert.AreEqual("Microsoft.Quantum.Providers.Honeywell", executionTarget?.PackageName);

            targetId = "QuantiNUUUm.targetId";
            executionTarget = AzureExecutionTarget.Create(targetId);
            Assert.AreEqual(targetId, executionTarget?.TargetId);
            Assert.AreEqual("Microsoft.Quantum.Providers.Quantinuum", executionTarget?.PackageName);

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

            // jobs list with count
            jobs = ExpectSuccess<IEnumerable<CloudJob>>(azureClient.GetJobListAsync(new MockChannel(), string.Empty, 1));
            Assert.AreEqual(1, jobs.Count());

            // jobs list with invalid filter
            jobs = ExpectSuccess<IEnumerable<CloudJob>>(azureClient.GetJobListAsync(new MockChannel(), "INVALID_FILTER"));
            Assert.AreEqual(0, jobs.Count());

            // jobs list with partial filter
            jobs = ExpectSuccess<IEnumerable<CloudJob>>(azureClient.GetJobListAsync(new MockChannel(), "JOB_ID"));
            Assert.AreEqual(2, jobs.Count());

            // jobs list with filter and count
            jobs = ExpectSuccess<IEnumerable<CloudJob>>(azureClient.GetJobListAsync(new MockChannel(), "JOB_ID", 1));
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
            azureWorkspace?.AddProviders("ionq", "honeywell", "quantinuum", "unrecognized");

            // get connection status to verify list of targets
            targets = ExpectSuccess<IEnumerable<TargetStatusInfo>>(azureClient.GetConnectionStatusAsync(new MockChannel()));
            // Above, we added 3 valid quantum execution targets, each of which contributes two targets (simulator and mock),
            // for a total of six targets.
            Assert.That.Enumerable(targets).HasCount(6);

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
            azureWorkspace?.AddProviders("ionq", "honeywell", "quantinuum");

            // Verify that IonQ job fails to compile (QPRGen0)
            ExpectSuccess<TargetStatusInfo>(azureClient.SetActiveTargetAsync(new MockChannel(), "ionq.mock"));
            ExpectError(AzureClientError.InvalidEntryPoint, azureClient.SubmitJobAsync(new MockChannel(), submissionContext, CancellationToken.None));

            // Verify that Honeywell job can be successfully submitted (QPRGen1)
            ExpectSuccess<TargetStatusInfo>(azureClient.SetActiveTargetAsync(new MockChannel(), "honeywell.mock"));
            var job = ExpectSuccess<CloudJob>(azureClient.SubmitJobAsync(new MockChannel(), submissionContext, CancellationToken.None));
            Assert.IsNotNull(job);

            // Verify that Quantinuum job can be successfully submitted (QPRGen1)
            ExpectSuccess<TargetStatusInfo>(azureClient.SetActiveTargetAsync(new MockChannel(), "quantinuum.mock"));
            job = ExpectSuccess<CloudJob>(azureClient.SubmitJobAsync(new MockChannel(), submissionContext, CancellationToken.None));
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public void TestLocations()
        {
            var services = Startup.CreateServiceProvider("Workspace");
            var azureClient = (AzureClient)services.GetService<IAzureClient>();

            // Locations with whitespace should be converted correctly
            _ = ExpectSuccess<IEnumerable<TargetStatusInfo>>(ConnectToWorkspaceAsync(azureClient, locationName: "Australia Central 2"));
            Assert.AreEqual("australiacentral2", azureClient.ActiveWorkspace?.Location);

            // No location provided should fail
            ExpectError(AzureClientError.NoWorkspaceLocation, ConnectToWorkspaceAsync(azureClient, locationName: ""));
            ExpectError(AzureClientError.NoWorkspaceLocation, ConnectToWorkspaceAsync(azureClient, locationName: "   "));

            // Invalid locations should fail
            ExpectError(AzureClientError.InvalidWorkspaceLocation, ConnectToWorkspaceAsync(azureClient, locationName: "#"));
            ExpectError(AzureClientError.InvalidWorkspaceLocation, ConnectToWorkspaceAsync(azureClient, locationName: "/test/"));
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

        [TestMethod]
        public void TestCloudJobExtensions()
        {
            // Note about currency formatting:
            //   It seems that there is a differency of implementations when using the "C" currency
            //   formatting accross different OSs.
            //   For example, in my dev box I was getting "R$ 12.00" and in the build agent 
            //   it was producing "R$12,00" even when explicitly passing the CultureInfo
            //   So instead of using the expected string literals, we are using 
            //      .ToString("C", CurrencyHelper.GetCultureInfoForCurrencyCode("USD")
            //   to guarantee consistency in the unit test.

            const string jobId = "myjobid";
            const string jobName = "myjobname";
            var jobStatus = JobStatus.Succeeded;
            const string jobProviderId = "microsoft";
            const string jobTarget = "microsoft.paralleltempering-parameterfree.cpu";
            var jobCreationTime =  new DateTimeOffset(2021, 08, 12, 01, 02, 03, TimeSpan.Zero);
            var jobBeginExecutionTime =  new DateTimeOffset(2021, 08, 12, 02, 02, 03, TimeSpan.Zero);
            var jobEndExecutionTime =  new DateTimeOffset(2021, 08, 12, 03, 02, 03, TimeSpan.Zero);
            var costEstimate = new MockCostEstimate("USD", new List<UsageEvent>(), 123.45f);
            var costEstimateString = 123.45f.ToString("C", CurrencyHelper.GetCultureInfoForCurrencyCode("USD"));

            // Test Cost Estimate formatting
            var cloudJob = new MockCloudJob();
            cloudJob.Details.CostEstimate = new MockCostEstimate("USD", new List<UsageEvent>(), 123.45f);
            Assert.AreEqual(123.45f.ToString("C", CurrencyHelper.GetCultureInfoForCurrencyCode("USD")), cloudJob.GetCostEstimateText());
            cloudJob.Details.CostEstimate = new MockCostEstimate("BRL", new List<UsageEvent>(), 12f);
            Assert.AreEqual(12f.ToString("C", CurrencyHelper.GetCultureInfoForCurrencyCode("BRL")), cloudJob.GetCostEstimateText());
            cloudJob.Details.CostEstimate = new MockCostEstimate("", new List<UsageEvent>(), 12f);
            Assert.AreEqual(12f.ToString("F2"), cloudJob.GetCostEstimateText());
            cloudJob.Details.CostEstimate = new MockCostEstimate("CustomCurrency", new List<UsageEvent>(), 12f);
            Assert.AreEqual($"CustomCurrency {12f:F2}", cloudJob.GetCostEstimateText());
            cloudJob.Details.CostEstimate = null;
            Assert.AreEqual("", cloudJob.GetCostEstimateText());

            // Test CloudJob to Dictionary
            cloudJob = new MockCloudJob(id: jobId);
            cloudJob.Details.Name = jobName;
            cloudJob.Details.Status = jobStatus;
            cloudJob.Details.ProviderId = jobProviderId;
            cloudJob.Details.Target = jobTarget;
            cloudJob.Details.CreationTime = jobCreationTime;
            cloudJob.Details.BeginExecutionTime = jobBeginExecutionTime;
            cloudJob.Details.EndExecutionTime = jobEndExecutionTime;
            cloudJob.Details.CostEstimate = costEstimate;
            var dictionary = cloudJob.ToDictionary();
            Assert.AreEqual(jobId, dictionary["id"]);
            Assert.AreEqual(jobName, dictionary["name"]);
            Assert.AreEqual(jobStatus.ToString(), dictionary["status"]);
            Assert.AreEqual(cloudJob.Uri.ToString(), dictionary["uri"]);
            Assert.AreEqual(jobProviderId, dictionary["provider"]);
            Assert.AreEqual(jobTarget, dictionary["target"]);
            Assert.AreEqual(cloudJob.Details.CreationTime, dictionary["creation_time"]);
            Assert.AreEqual(cloudJob.Details.BeginExecutionTime, dictionary["begin_execution_time"]);
            Assert.AreEqual(cloudJob.Details.EndExecutionTime, dictionary["end_execution_time"]);
            Assert.AreEqual(costEstimateString, dictionary["cost_estimate"]);
            
            // Test CloudJob to JupyterTable
            var cloudJobs = new List<CloudJob> { cloudJob, new MockCloudJob() };
            var table = cloudJobs.ToJupyterTable();
            var expectedValues = new List<(string, string)>
            {
                ("Job Name", jobName),
                ("Job ID", $"<a href=\"{cloudJob.Uri}\" target=\"_blank\">{jobId}</a>"),
                ("Job Status", jobStatus.ToString()),
                ("Target", jobTarget),
                ("Creation Time", jobCreationTime.ToString()),
                ("Begin Execution Time", jobBeginExecutionTime.ToString()),
                ("End Execution Time", jobEndExecutionTime.ToString()),
                ("Cost Estimate", costEstimateString),
            };
            Assert.AreEqual(cloudJobs.Count, table.Rows.Count);
            Assert.AreEqual(expectedValues.Count, table.Columns.Count);
            foreach ((var expected, var actual) in Enumerable.Zip(expectedValues, table.Columns))
            {
                Assert.AreEqual(expected.Item1, actual.Item1);
                Assert.AreEqual(expected.Item2, actual.Item2(cloudJob));
            }
        }
    }
}
