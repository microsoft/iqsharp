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
        }

        /// <inheritdoc />
        public event EventHandler<SimulatorPerformanceArgs>? OnSimulatorPerformanceAvailable;

        /// <inheritdoc />
        public event EventHandler<KernelPerformanceArgs>? OnKernelPerformanceAvailable;

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
        internal void ReportSimulatorPerformance(SimulatorPerformanceArgs args)
        {
            this.OnSimulatorPerformanceAvailable?.Invoke(this, args);
        }

    }
}
