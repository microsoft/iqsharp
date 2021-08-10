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
