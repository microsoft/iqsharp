﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
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
        private IMetadataController MetadataController { get; }
        private bool IsPythonUserAgent => MetadataController?.UserAgent?.StartsWith("qsharp.py") ?? false;
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

        /// <summary>
        /// Creates an <see cref="AzureClient"/> object that provides methods for
        /// interacting with an Azure Quantum workspace.
        /// </summary>
        /// <param name="engine">The execution engine for interaction with Jupyter.</param>
        /// <param name="references">The references to use when compiling Q# code.</param>
        /// <param name="entryPointGenerator">The generator of entry points for Azure Quantum execution.</param>
        /// <param name="metadataController">The metadata controller to use when compiling Q# code.</param>
        /// <param name="logger">The logger to use for diagnostic information.</param>
        /// <param name="eventService">The event service for the IQ# kernel.</param>
        public AzureClient(
            IExecutionEngine engine,
            IReferences references,
            IEntryPointGenerator entryPointGenerator,
            IMetadataController metadataController,
            ILogger<AzureClient> logger,
            IEventService eventService)
        {
            References = references;
            EntryPointGenerator = entryPointGenerator;
            MetadataController = metadataController;
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
                baseEngine.RegisterDisplayEncoder(new AzureClientErrorToHtmlEncoder());
                baseEngine.RegisterDisplayEncoder(new AzureClientErrorToTextEncoder());
                baseEngine.RegisterDisplayEncoder(new DeviceCodeResultToHtmlEncoder());
                baseEngine.RegisterDisplayEncoder(new DeviceCodeResultToTextEncoder());
            }
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> ConnectAsync(IChannel channel,
            string subscriptionId,
            string resourceGroupName,
            string workspaceName,
            string storageAccountConnectionString,
            string location,
            bool refreshCredentials = false,
            CancellationToken? cancellationToken = null)
        {
            var connectionResult = await ConnectToWorkspaceAsync(channel, subscriptionId, resourceGroupName, workspaceName, location, refreshCredentials, cancellationToken);
            if (connectionResult.Status != ExecuteStatus.Ok)
            {
                return connectionResult;
            }

            if (ActiveWorkspace == null)
            {
                return AzureClientError.WorkspaceNotFound.ToExecutionResult();
            }

            ConnectionString = storageAccountConnectionString;
            ActiveTarget = null;
            MostRecentJobId = string.Empty;

            channel.Stdout($"Connected to Azure Quantum workspace {ActiveWorkspace.Name} in location {ActiveWorkspace.Location}.");

            if (ValidExecutionTargets.Count() == 0)
            {
                channel.Stderr($"No valid Q# execution targets found in Azure Quantum workspace {ActiveWorkspace.Name}.");
            }

            return ValidExecutionTargets.ToExecutionResult();
        }

        private async Task<ExecutionResult> ConnectToWorkspaceAsync(IChannel channel,
            string subscriptionId,
            string resourceGroupName,
            string workspaceName,
            string location,
            bool refreshCredentials,
            CancellationToken? cancellationToken = null)
        {
            var azureEnvironment = AzureEnvironment.Create(subscriptionId, Logger);
            IAzureWorkspace? workspace = null;
            try
            {
                workspace = await azureEnvironment.GetAuthenticatedWorkspaceAsync(channel, Logger, resourceGroupName, workspaceName, location, refreshCredentials, cancellationToken);
            }
            catch (TaskCanceledException tce)
            {
                throw tce;
            }
            catch (Exception e)
            {
                channel.Stderr($"The connection to the Azure Quantum workspace could not be completed. Please check the provided parameters and try again.");
                channel.Stderr($"Error details: {e.Message}");
                return AzureClientError.WorkspaceNotFound.ToExecutionResult();
            }

            if (workspace == null)
            {
                return AzureClientError.AuthenticationFailed.ToExecutionResult();
            }

            var providers = await workspace.GetProvidersAsync();
            if (providers == null)
            {
                channel.Stderr($"The Azure Quantum workspace {workspace.Name} in location {workspace.Location} could not be reached. " +
                    "Please check the provided parameters and try again.");
                return AzureClientError.WorkspaceNotFound.ToExecutionResult();
            }

            ActiveWorkspace = workspace;
            AvailableProviders = providers;

            return ExecuteStatus.Ok.ToExecutionResult();
        }

        private async Task<ExecutionResult> RefreshConnectionAsync(IChannel channel, CancellationToken? cancellationToken = null)
        {
            if (ActiveWorkspace == null)
            {
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            return await ConnectToWorkspaceAsync(
                channel,
                ActiveWorkspace.SubscriptionId ?? string.Empty,
                ActiveWorkspace.ResourceGroup ?? string.Empty,
                ActiveWorkspace.Name ?? string.Empty,
                ActiveWorkspace.Location ?? string.Empty,
                refreshCredentials: false,
                cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetConnectionStatusAsync(IChannel channel)
        {
            if (ActiveWorkspace == null || AvailableProviders == null)
            {
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            var connectionResult = await RefreshConnectionAsync(channel);
            if (connectionResult.Status != ExecuteStatus.Ok)
            {
                return connectionResult;
            }

            channel.Stdout($"Connected to Azure Quantum workspace {ActiveWorkspace.Name} in location {ActiveWorkspace.Location}.");

            return ValidExecutionTargets.ToExecutionResult();
        }

        private async Task<ExecutionResult> SubmitOrExecuteJobAsync(
            IChannel channel,
            AzureSubmissionContext submissionContext,
            bool execute,
            CancellationToken cancellationToken)
        {
            if (ActiveWorkspace == null)
            {
                channel.Stderr($"Please call {GetCommandDisplayName("connect")} before submitting a job.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            if (ActiveTarget == null)
            {
                channel.Stderr($"Please call {GetCommandDisplayName("target")} before submitting a job.");
                return AzureClientError.NoTarget.ToExecutionResult();
            }

            if (string.IsNullOrEmpty(submissionContext.OperationName))
            {
                channel.Stderr($"Please pass a valid Q# operation name to {GetCommandDisplayName(execute ? "execute" : "submit")}.");
                return AzureClientError.NoOperationName.ToExecutionResult();
            }

            var connectionResult = await RefreshConnectionAsync(channel);
            if (connectionResult.Status != ExecuteStatus.Ok)
            {
                return connectionResult;
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
                entryPoint = EntryPointGenerator.Generate(submissionContext.OperationName, ActiveTarget.TargetId, ActiveTarget.RuntimeCapability);
            }
            catch (UnsupportedOperationException)
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

            // If the command was not %azure.execute, simply return the job status.
            if (!execute)
            {
                return await GetJobStatusAsync(channel, MostRecentJobId);
            }

            // If the command was %azure.execute, wait for the job to complete and return the job output.
            channel.Stdout($"Waiting up to {submissionContext.ExecutionTimeout} seconds for Azure Quantum job to complete...");

            using var executionTimeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(submissionContext.ExecutionTimeout));
            using var executionCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(executionTimeoutTokenSource.Token, cancellationToken);
            {
                try
                {
                    CloudJob? cloudJob = null;
                    while (cloudJob == null || cloudJob.InProgress)
                    {
                        executionCancellationTokenSource.Token.ThrowIfCancellationRequested();
                        await Task.Delay(TimeSpan.FromSeconds(submissionContext.ExecutionPollingInterval), executionCancellationTokenSource.Token);
                        cloudJob = await ActiveWorkspace.GetJobAsync(MostRecentJobId);
                        channel.Stdout($"[{DateTime.Now.ToLongTimeString()}] Current job status: {cloudJob?.Status ?? "Unknown"}");
                    }
                }
                catch (Exception e) when (e is TaskCanceledException || e is OperationCanceledException)
                {
                    Logger?.LogInformation($"Operation canceled while waiting for job execution to complete: {e.Message}");
                }
            }

            return await GetJobResultAsync(channel, MostRecentJobId);
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> SubmitJobAsync(IChannel channel, AzureSubmissionContext submissionContext, CancellationToken? cancellationToken = null) =>
            await SubmitOrExecuteJobAsync(channel, submissionContext, execute: false, cancellationToken ?? CancellationToken.None);

        /// <inheritdoc/>
        public async Task<ExecutionResult> ExecuteJobAsync(IChannel channel, AzureSubmissionContext submissionContext, CancellationToken? cancellationToken = null) =>
            await SubmitOrExecuteJobAsync(channel, submissionContext, execute: true, cancellationToken ?? CancellationToken.None);

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetActiveTargetAsync(IChannel channel)
        {
            if (AvailableProviders == null)
            {
                channel.Stderr($"Please call {GetCommandDisplayName("connect")} before getting the execution target.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            if (ActiveTarget == null)
            {
                channel.Stderr($"No execution target has been specified. To specify one, call {GetCommandDisplayName("target")} with the target ID.");
                channel.Stdout($"Available execution targets: {ValidExecutionTargetsDisplayText}");
                return AzureClientError.NoTarget.ToExecutionResult();
            }

            var connectionResult = await RefreshConnectionAsync(channel);
            if (connectionResult.Status != ExecuteStatus.Ok)
            {
                return connectionResult;
            }

            channel.Stdout($"Current execution target: {ActiveTarget.TargetId}");
            channel.Stdout($"Available execution targets: {ValidExecutionTargetsDisplayText}");

            return AvailableTargets.First(target => target.Id == ActiveTarget.TargetId).ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> SetActiveTargetAsync(IChannel channel, string targetId)
        {
            if (ActiveWorkspace == null || AvailableProviders == null)
            {
                channel.Stderr($"Please call {GetCommandDisplayName("connect")} before setting an execution target.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            var connectionResult = await RefreshConnectionAsync(channel);
            if (connectionResult.Status != ExecuteStatus.Ok)
            {
                return connectionResult;
            }

            // Validate that this target is valid in the workspace.
            if (!AvailableTargets.Any(target => targetId == target.Id))
            {
                channel.Stderr($"Target {targetId} is not available in the current Azure Quantum workspace.");
                channel.Stdout($"Available execution targets: {ValidExecutionTargetsDisplayText}");
                return AzureClientError.InvalidTarget.ToExecutionResult();
            }

            // Validate that we know which package to load for this target.
            var executionTarget = ActiveWorkspace.CreateExecutionTarget(targetId);
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

            channel.Stdout($"Active target is now {ActiveTarget.TargetId}");

            return AvailableTargets.First(target => target.Id == ActiveTarget.TargetId).ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetJobResultAsync(IChannel channel, string jobId)
        {
            if (ActiveWorkspace == null)
            {
                channel.Stderr($"Please call {GetCommandDisplayName("connect")} before getting job results.");
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

            var connectionResult = await RefreshConnectionAsync(channel);
            if (connectionResult.Status != ExecuteStatus.Ok)
            {
                return connectionResult;
            }

            var job = await ActiveWorkspace.GetJobAsync(jobId);
            if (job == null)
            {
                channel.Stderr($"Job ID {jobId} not found in current Azure Quantum workspace.");
                return AzureClientError.JobNotFound.ToExecutionResult();
            }

            if (job.InProgress)
            {
                channel.Stderr($"Job ID {jobId} has not completed. To check the status, call {GetCommandDisplayName("status")} with the job ID.");
                return AzureClientError.JobNotCompleted.ToExecutionResult();
            }
            else if (job.Failed)
            {
                channel.Stderr($"Job ID {jobId} failed or was cancelled with the message: {job.Details.ErrorData.Message}");
                return AzureClientError.JobFailedOrCancelled.ToExecutionResult();
            }

            try
            {
                var request = WebRequest.Create(job.Details.OutputDataUri);
                using var responseStream = request.GetResponse().GetResponseStream();
                return responseStream.ToHistogram(Logger).ToExecutionResult();
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
                channel.Stderr($"Please call {GetCommandDisplayName("connect")} before getting job status.");
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

            var connectionResult = await RefreshConnectionAsync(channel);
            if (connectionResult.Status != ExecuteStatus.Ok)
            {
                return connectionResult;
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
        public async Task<ExecutionResult> GetJobListAsync(IChannel channel, string filter)
        {
            if (ActiveWorkspace == null)
            {
                channel.Stderr($"Please call {GetCommandDisplayName("connect")} before listing jobs.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            var connectionResult = await RefreshConnectionAsync(channel);
            if (connectionResult.Status != ExecuteStatus.Ok)
            {
                return connectionResult;
            }

            var jobs = await ActiveWorkspace.ListJobsAsync() ?? new List<CloudJob>();
            if (jobs.Count() == 0)
            {
                channel.Stderr("No jobs found in current Azure Quantum workspace.");
            }
            else
            {
                jobs = jobs.Where(job => job.Matches(filter));
                if (jobs.Count() == 0)
                {
                    channel.Stderr($"No jobs matching \"{filter}\" found in current Azure Quantum workspace.");
                }
            }
            
            return jobs.ToExecutionResult();
        }

        public async Task<ExecutionResult> GetQuotaListAsync(IChannel channel)
        {
            if (ActiveWorkspace == null)
            {
                channel.Stderr($"Please call {GetCommandDisplayName("connect")} before reading quota information.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            var connectionResult = await RefreshConnectionAsync(channel);
            if (connectionResult.Status != ExecuteStatus.Ok)
            {
                return connectionResult;
            }

            var quotas = await ActiveWorkspace.ListQuotasAsync() ?? new List<QuotaInfo>();

            if (quotas.Count() == 0)
            {
                channel.Stdout("No quota information found in current Azure Quantum workspace.");
            }
            
            return quotas.ToExecutionResult();
        }

        private string GetCommandDisplayName(string commandName) =>
            IsPythonUserAgent ? $"qsharp.azure.{commandName}()" : $"%azure.{commandName}";
    }
}
