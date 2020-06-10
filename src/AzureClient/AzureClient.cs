// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Quantum;
using Microsoft.Azure.Quantum.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.Simulation.Common;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <inheritdoc/>
    public class AzureClient : IAzureClient
    {
        internal IAzureWorkspace? ActiveWorkspace { get; private set; }
        private ILogger<AzureClient> Logger { get; }
        private IReferences References { get; }
        private IEntryPointGenerator EntryPointGenerator { get; }
        private string ConnectionString { get; set; } = string.Empty;
        private AzureExecutionTarget? ActiveTarget { get; set; }
        private string MostRecentJobId { get; set; } = string.Empty;
        private IEnumerable<ProviderStatus>? AvailableProviders { get; set; }
        private IEnumerable<TargetStatus>? AvailableTargets => AvailableProviders?.SelectMany(provider => provider.Targets);
        private IEnumerable<TargetStatus>? ValidExecutionTargets => AvailableTargets?.Where(target => AzureExecutionTarget.IsValid(target.Id));
        private string ValidExecutionTargetsDisplayText =>
            ValidExecutionTargets == null
            ? "(no execution targets available)"
            : string.Join(", ", ValidExecutionTargets.Select(target => target.Id));

        public AzureClient(
            IExecutionEngine engine,
            IReferences references,
            IEntryPointGenerator entryPointGenerator,
            ILogger<AzureClient> logger,
            IEventService eventService)
        {
            References = references;
            EntryPointGenerator = entryPointGenerator;
            Logger = logger;
            eventService?.TriggerServiceInitialized<IAzureClient>(this);

            if (engine is BaseEngine baseEngine)
            {
                baseEngine.RegisterDisplayEncoder(new CloudJobToHtmlEncoder());
                baseEngine.RegisterDisplayEncoder(new CloudJobToTextEncoder());
                baseEngine.RegisterDisplayEncoder(new TargetStatusToHtmlEncoder());
                baseEngine.RegisterDisplayEncoder(new TargetStatusToTextEncoder());
                baseEngine.RegisterDisplayEncoder(new HistogramToHtmlEncoder());
                baseEngine.RegisterDisplayEncoder(new HistogramToTextEncoder());
            }
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> ConnectAsync(
            IChannel channel,
            string subscriptionId,
            string resourceGroupName,
            string workspaceName,
            string storageAccountConnectionString,
            bool refreshCredentials = false)
        {
            ConnectionString = storageAccountConnectionString;

            var azureEnvironment = AzureEnvironment.Create(subscriptionId);
            ActiveWorkspace = await azureEnvironment.GetAuthenticatedWorkspaceAsync(channel, resourceGroupName, workspaceName, refreshCredentials);
            if (ActiveWorkspace == null)
            {
                return AzureClientError.AuthenticationFailed.ToExecutionResult();
            }

            AvailableProviders = await ActiveWorkspace.GetProvidersAsync();
            if (AvailableProviders == null)
            {
                return AzureClientError.WorkspaceNotFound.ToExecutionResult();
            }

            channel.Stdout($"Connected to Azure Quantum workspace {ActiveWorkspace.Name}.");

            return ValidExecutionTargets.ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetConnectionStatusAsync(IChannel channel)
        {
            if (ActiveWorkspace == null || AvailableProviders == null)
            {
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            channel.Stdout($"Connected to Azure Quantum workspace {ActiveWorkspace.Name}.");

            return ValidExecutionTargets.ToExecutionResult();
        }

        private async Task<ExecutionResult> SubmitOrExecuteJobAsync(IChannel channel, AzureSubmissionContext submissionContext, bool execute)
        {
            if (ActiveWorkspace == null)
            {
                channel.Stderr("Please call %azure.connect before submitting a job.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            if (ActiveTarget == null)
            {
                channel.Stderr("Please call %azure.target before submitting a job.");
                return AzureClientError.NoTarget.ToExecutionResult();
            }

            if (string.IsNullOrEmpty(submissionContext.OperationName))
            {
                var commandName = execute ? "%azure.execute" : "%azure.submit";
                channel.Stderr($"Please pass a valid Q# operation name to {commandName}.");
                return AzureClientError.NoOperationName.ToExecutionResult();
            }

            var machine = ActiveWorkspace.CreateQuantumMachine(ActiveTarget.TargetId, ConnectionString);
            if (machine == null)
            {
                // We should never get here, since ActiveTarget should have already been validated at the time it was set.
                channel.Stderr($"Unexpected error while preparing job for execution on target {ActiveTarget.TargetId}.");
                return AzureClientError.InvalidTarget.ToExecutionResult();
            }

            channel.Stdout($"Submitting {submissionContext.OperationName} to target {ActiveTarget.TargetId}...");

            IEntryPoint? entryPoint = null;
            try
            {
                entryPoint = EntryPointGenerator.Generate(submissionContext.OperationName, ActiveTarget.TargetId);
            }
            catch (UnsupportedOperationException e)
            {
                channel.Stderr($"{submissionContext.OperationName} is not a recognized Q# operation name.");
                return AzureClientError.UnrecognizedOperationName.ToExecutionResult();
            }
            catch (CompilationErrorsException e)
            {
                channel.Stderr($"The Q# operation {submissionContext.OperationName} could not be compiled as an entry point for job execution.");
                foreach (var message in e.Errors) channel.Stderr(message);
                return AzureClientError.InvalidEntryPoint.ToExecutionResult();
            }

            try
            {
                var job = await entryPoint.SubmitAsync(machine, submissionContext);
                channel.Stdout($"Job successfully submitted for {submissionContext.Shots} shots.");
                channel.Stdout($"   Job name: {submissionContext.FriendlyName}");
                channel.Stdout($"   Job ID: {job.Id}");
                MostRecentJobId = job.Id;
            }
            catch (ArgumentException e)
            {
                channel.Stderr($"Failed to parse all expected parameters for Q# operation {submissionContext.OperationName}.");
                channel.Stderr(e.Message);
                return AzureClientError.JobSubmissionFailed.ToExecutionResult();
            }
            catch (Exception e)
            {
                channel.Stderr($"Failed to submit Q# operation {submissionContext.OperationName} for execution.");
                channel.Stderr(e.InnerException?.Message ?? e.Message);
                return AzureClientError.JobSubmissionFailed.ToExecutionResult();
            }

            if (!execute)
            {
                return await GetJobStatusAsync(channel, MostRecentJobId);
            }

            channel.Stdout($"Waiting up to {submissionContext.ExecutionTimeout} seconds for Azure Quantum job to complete...");

            using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(submissionContext.ExecutionTimeout)))
            {
                CloudJob? cloudJob = null;
                do
                {
                    // TODO: Once jupyter-core supports interrupt requests (https://github.com/microsoft/jupyter-core/issues/55),
                    //       handle Jupyter kernel interrupt here and break out of this loop 
                    await Task.Delay(TimeSpan.FromSeconds(submissionContext.ExecutionPollingInterval));
                    if (cts.IsCancellationRequested) break;
                    cloudJob = await ActiveWorkspace.GetJobAsync(MostRecentJobId);
                    channel.Stdout($"[{DateTime.Now.ToLongTimeString()}] Current job status: {cloudJob?.Status ?? "Unknown"}");
                }
                while (cloudJob == null || cloudJob.InProgress);
            }

            return await GetJobResultAsync(channel, MostRecentJobId);
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> SubmitJobAsync(IChannel channel, AzureSubmissionContext submissionContext) =>
            await SubmitOrExecuteJobAsync(channel, submissionContext, execute: false);

        /// <inheritdoc/>
        public async Task<ExecutionResult> ExecuteJobAsync(IChannel channel, AzureSubmissionContext submissionContext) =>
            await SubmitOrExecuteJobAsync(channel, submissionContext, execute: true);

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetActiveTargetAsync(IChannel channel)
        {
            if (AvailableProviders == null)
            {
                channel.Stderr("Please call %azure.connect before getting the execution target.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            if (ActiveTarget == null)
            {
                channel.Stderr("No execution target has been specified. To specify one, run:\n%azure.target <target ID>");
                channel.Stdout($"Available execution targets: {ValidExecutionTargetsDisplayText}");
                return AzureClientError.NoTarget.ToExecutionResult();
            }

            channel.Stdout($"Current execution target: {ActiveTarget.TargetId}");
            channel.Stdout($"Available execution targets: {ValidExecutionTargetsDisplayText}");
            return ActiveTarget.TargetId.ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> SetActiveTargetAsync(IChannel channel, string targetId)
        {
            if (AvailableProviders == null)
            {
                channel.Stderr("Please call %azure.connect before setting an execution target.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            // Validate that this target is valid in the workspace.
            if (!AvailableTargets.Any(target => targetId == target.Id))
            {
                channel.Stderr($"Target {targetId} is not available in the current Azure Quantum workspace.");
                channel.Stdout($"Available execution targets: {ValidExecutionTargetsDisplayText}");
                return AzureClientError.InvalidTarget.ToExecutionResult();
            }

            // Validate that we know which package to load for this target.
            var executionTarget = AzureExecutionTarget.Create(targetId);
            if (executionTarget == null)
            {
                channel.Stderr($"Target {targetId} does not support executing Q# jobs.");
                channel.Stdout($"Available execution targets: {ValidExecutionTargetsDisplayText}");
                return AzureClientError.InvalidTarget.ToExecutionResult();
            }

            // Set the active target and load the package.
            ActiveTarget = executionTarget;

            channel.Stdout($"Loading package {ActiveTarget.PackageName} and dependencies...");
            await References.AddPackage(ActiveTarget.PackageName);

            return $"Active target is now {ActiveTarget.TargetId}".ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetJobResultAsync(IChannel channel, string jobId)
        {
            if (ActiveWorkspace == null)
            {
                channel.Stderr("Please call %azure.connect before getting job results.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            if (string.IsNullOrEmpty(jobId))
            {
                if (string.IsNullOrEmpty(MostRecentJobId))
                {
                    channel.Stderr("No job ID was specified. Please submit a job first or specify a job ID.");
                    return AzureClientError.JobNotFound.ToExecutionResult();
                }

                jobId = MostRecentJobId;
            }

            var job = await ActiveWorkspace.GetJobAsync(jobId);
            if (job == null)
            {
                channel.Stderr($"Job ID {jobId} not found in current Azure Quantum workspace.");
                return AzureClientError.JobNotFound.ToExecutionResult();
            }

            if (!job.Succeeded || string.IsNullOrEmpty(job.Details.OutputDataUri))
            {
                channel.Stderr($"Job ID {jobId} has not completed. To check the status, use:\n   %azure.status {jobId}");
                return AzureClientError.JobNotCompleted.ToExecutionResult();
            }

            try
            {
                var request = WebRequest.Create(job.Details.OutputDataUri);
                using var responseStream = request.GetResponse().GetResponseStream();
                return responseStream.ToHistogram().ToExecutionResult();
            }
            catch (Exception e)
            {
                channel.Stderr($"Failed to retrieve results for job ID {jobId}.");
                Logger?.LogError(e, $"Failed to download the job output for the specified Azure Quantum job: {e.Message}");
                return AzureClientError.JobOutputDownloadFailed.ToExecutionResult();
            }
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetJobStatusAsync(IChannel channel, string jobId)
        {
            if (ActiveWorkspace == null)
            {
                channel.Stderr("Please call %azure.connect before getting job status.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            if (string.IsNullOrEmpty(jobId))
            {
                if (string.IsNullOrEmpty(MostRecentJobId))
                {
                    channel.Stderr("No job ID was specified. Please submit a job first or specify a job ID.");
                    return AzureClientError.JobNotFound.ToExecutionResult();
                }

                jobId = MostRecentJobId;
            }

            var job = await ActiveWorkspace.GetJobAsync(jobId);
            if (job == null)
            {
                channel.Stderr($"Job ID {jobId} not found in current Azure Quantum workspace.");
                return AzureClientError.JobNotFound.ToExecutionResult();
            }

            return job.ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetJobListAsync(IChannel channel)
        {
            if (ActiveWorkspace == null)
            {
                channel.Stderr("Please call %azure.connect before listing jobs.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            var jobs = await ActiveWorkspace.ListJobsAsync();
            if (jobs == null || jobs.Count() == 0)
            {
                channel.Stderr("No jobs found in current Azure Quantum workspace.");
                return AzureClientError.JobNotFound.ToExecutionResult();
            }

            return jobs.ToExecutionResult();
        }
    }
}
