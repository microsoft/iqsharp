// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Quantum.IQSharp
{
    
    /// <summary>
    ///     Represents details about the performance of a simulator used from
    ///     the IQ# kernel.
    /// </summary>
    public class SimulatorPerformanceArgs : EventArgs
    {
        public SimulatorPerformanceArgs(string simulatorName, int nQubits, TimeSpan duration)
        {
            this.SimulatorName = simulatorName;
            this.NQubits = nQubits;
            this.Duration = duration;
        }

        /// <summary>
        ///     The fully-qualified name of the simulator whose performance
        ///     is described by these event arguments.
        /// </summary>
        public string SimulatorName { get; }

        /// <summary>
        ///     The maximum number of qubits simulated by the simulator whose
        ///     performance is described by these event arguments.
        /// </summary>
        public int NQubits { get; }

        /// <summary>
        ///     The total time that the simulator whose performance
        ///     is described by these event arguments ran for.
        /// </summary>
        public TimeSpan Duration { get; }
    }

    /// <summary>
    ///      Represents details about the performance of the IQ# kernel process
    ///      itself.
    /// </summary>
    public class KernelPerformanceArgs : EventArgs
    {
        public KernelPerformanceArgs(long managedRamUsed, long totalRamUsed)
        {
            this.ManagedRamUsed = managedRamUsed;
            this.TotalRamUsed = totalRamUsed;
        }

        /// <summary>
        ///      The approximate amount of RAM (in bytes) used by managed code
        ///      in the kernel process.
        /// </summary>
        public long ManagedRamUsed { get; }

        /// <summary>
        ///      The approximate amount of RAM (in bytes) used by all code in
        ///      the IQ# kernel process.
        /// </summary>
        public long TotalRamUsed { get; }
    }

    public class TaskPerformanceArgs : EventArgs
    {
        public TaskPerformanceArgs(ITaskReporter task, string statusDescription, string statusId, TimeSpan timeSinceTaskStart)
        {
            this.Task = task;
            this.StatusDescription = statusDescription;
            this.StatusId = statusId;
            this.TimeSinceTaskStart = timeSinceTaskStart;
        }

        public ITaskReporter Task { get; }
        public string StatusDescription { get; }
        public string StatusId { get; }
        public TimeSpan TimeSinceTaskStart { get; }

        public override string ToString() =>
            $"[T+{TimeSinceTaskStart.TotalMilliseconds}ms] {Task.Description}: {StatusDescription}";
    }

    public class TaskCompleteArgs : EventArgs
    {
        public TaskCompleteArgs(ITaskReporter task, TimeSpan timeSinceTaskStart)
        {
            this.Task = task;
            this.TimeSinceTaskStart = timeSinceTaskStart;
        }

        public ITaskReporter Task { get; }
        public TimeSpan TimeSinceTaskStart { get; }

        public override string ToString() =>
            $"[T+{TimeSinceTaskStart.TotalMilliseconds}ms] {Task.Description} complete.";
    }

    /// <summary>
    ///      An object that can be used to report progress through a given task.
    ///      The task is considered complete when this object is disposed.
    /// </summary>
    public interface ITaskReporter : IDisposable
    {
        public ITaskReporter? Parent { get; }
        public string Description { get; }
        public string Id { get; }
        public TimeSpan TimeSinceStart { get; }
        public TimeSpan TotalTimeSinceStart { get; }

        public void ReportStatus(string description, string id);
        public ITaskReporter BeginSubtask(string description, string id);

        public int Depth =>
            Parent == null
            ? 0
            : 1 + Parent.Depth;
    }

    /// <summary>
    ///      A service for reporting kernel performance, and collecting
    ///      performance reports from other kernel services.
    /// </summary>
    public interface IPerformanceMonitor
    {
        /// <summary>
        ///     Forces a report of current kernel performance data to be
        ///     emitted.
        /// </summary>
        public void Report();

        public bool EnableBackgroundReporting { get; set; }

        /// <summary>
        ///     Raised when a simulator reports information about its
        ///     performance.
        /// </summary>
        public event EventHandler<SimulatorPerformanceArgs>? OnSimulatorPerformanceAvailable;

        /// <summary>
        ///     Raised when information about kernel performance (e.g. RAM
        ///     usage) is available.
        /// </summary>
        public event EventHandler<KernelPerformanceArgs>? OnKernelPerformanceAvailable;

        public event EventHandler<TaskPerformanceArgs>? OnTaskPerformanceAvailable;
        public event EventHandler<TaskCompleteArgs>? OnTaskCompleteAvailable;

        /// <summary>
        ///      Begins performance monitoring. Does nothing if performance
        ///      monitoring has already been started.
        /// </summary>
        public void Start();

        /// <summary>
        ///      Returns an object that can be used to report progress through
        ///      a given task.
        /// </summary>
        public ITaskReporter BeginTask(string description, string id);

    }
}
