// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable
using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Quantum.IQSharp
{
    public class DeviceCapabilitiesEvent : Event<DeviceCapabilitiesArgs>
    {}

    #if NET5_0 || NET6_0 || NET5_0_OR_GREATER
    public record DeviceCapabilitiesArgs
    #else
    public class DeviceCapabilitiesArgs
    #endif
    {
        public uint? NProcessors { get; set; } = null;
        public uint? TotalMemoryInGiB { get; set; } = null;
    }

    public class PerformanceMonitor : IPerformanceMonitor
    {
        protected class TaskReporter : ITaskReporter
        {
            private readonly PerformanceMonitor monitor;
            private readonly Stopwatch stopwatch = new Stopwatch();
            private readonly TimeSpan startedAt;

            internal TaskReporter(PerformanceMonitor monitor, ITaskReporter? parent, string description, string id)
            {
                this.monitor = monitor;
                this.Parent = parent;
                this.Description = description;
                this.Id = id;

                startedAt = parent?.TimeSinceStart ?? TimeSpan.Zero;

                stopwatch.Start();
            }

            public string Description { get; }
            public string Id { get; }
            public ITaskReporter? Parent { get; }

            public TimeSpan TimeSinceStart => stopwatch.Elapsed;
            public TimeSpan TotalTimeSinceStart => TimeSinceStart + startedAt;

            public ITaskReporter BeginSubtask(string description, string id) =>
                new TaskReporter(monitor, this, description, id);

            public void Dispose()
            {
                stopwatch.Stop();
                monitor.OnTaskCompleteAvailable?.Invoke(this.monitor, new TaskCompleteArgs(
                    this, ((ITaskReporter)this).TotalTimeSinceStart
                ));
            }

            public void ReportStatus(string description, string id) =>
                monitor.OnTaskPerformanceAvailable?.Invoke(this.monitor, new TaskPerformanceArgs(
                    this,
                    description,
                    id,
                    ((ITaskReporter)this).TotalTimeSinceStart
                ));
        }

        private bool alive = false;
        private Thread? thread = null;

        public bool EnableBackgroundReporting
        {
            get => alive;
            set
            {
                if (alive == value)
                {
                    // No changes needed, return.
                    return;
                }

                if (alive && !value)
                {
                    // Running, but need to stop.
                    Stop();
                }
                else
                {
                    // Stopped, but need to run.
                    Start();
                }
            }
        }

        public PerformanceMonitor(IEventService? eventService = null, ILogger<PerformanceMonitor>? logger = null)
        {
            eventService?.TriggerServiceInitialized<IPerformanceMonitor>(this);
            // Get client capabilities in the background so as to not block the
            // constructor.
            new Thread(async () =>
            {
                var args = new DeviceCapabilitiesArgs
                {
                    NProcessors = (uint)System.Environment.ProcessorCount,
                    // Round to nearest GiB so as to avoid collecting too much
                    // entropy.
                    TotalMemoryInGiB = await PlatformUtils.GetTotalMemory(logger) is ulong nBytes
                                      ? (uint?)(nBytes / 1024 / 1024 / 1024)
                                      : (uint?)null
                };
                logger?.LogInformation("Reporting device capabilities: {Capabilities}", args);
                eventService?.Trigger<DeviceCapabilitiesEvent, DeviceCapabilitiesArgs>(args);
            }).Start();

            OnTaskPerformanceAvailable += (sender, args) =>
            {
                // TODO: Demote to debug.
                logger?.LogInformation("[{TimeSinceStart}] {Task} / {Description}", args.TimeSinceTaskStart, args.Task.Description, args.StatusDescription);
            };
        }

        /// <inheritdoc />
        public event EventHandler<SimulatorPerformanceArgs>? OnSimulatorPerformanceAvailable;

        /// <inheritdoc />
        public event EventHandler<KernelPerformanceArgs>? OnKernelPerformanceAvailable;
        public event EventHandler<TaskPerformanceArgs>? OnTaskPerformanceAvailable;
        public event EventHandler<TaskCompleteArgs>? OnTaskCompleteAvailable;

        /// <inheritdoc />
        public void Report()
        {
            var managedRamUsed = GC.GetTotalMemory(forceFullCollection: false);
            var totalRamUsed = Process.GetCurrentProcess().WorkingSet64;
            OnKernelPerformanceAvailable?.Invoke(this, new KernelPerformanceArgs(
                managedRamUsed, totalRamUsed
            ));
        }

        /// <inheritdoc />
        public void Start()
        {
            if (!alive)
            {
                alive = true;
                thread = new Thread(EventLoop);
                thread.Start();
            }
        }

        /// <inheritdoc />
        protected void Join() => thread?.Join();

        /// <inheritdoc />
        protected void Stop()
        {
            alive = false;
            thread?.Interrupt();
            Join();
            thread = null;
        }

        protected void EventLoop()
        {
            while (alive)
            {
                Report();
                Thread.Sleep(15000);
            }
        }

        /// <summary>
        ///      Given a new simulator performance record, emits an event with
        ///      that performance data.
        /// </summary>
        public void ReportSimulatorPerformance(SimulatorPerformanceArgs args)
        {
            this.OnSimulatorPerformanceAvailable?.Invoke(this, args);
        }

        public ITaskReporter BeginTask(string description, string id) =>
            new TaskReporter(this, null, description, id);
    }
}
