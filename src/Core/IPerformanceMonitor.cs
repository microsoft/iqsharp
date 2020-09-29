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

    public class KernelPerformanceArgs : EventArgs
    {
        public KernelPerformanceArgs(long managedRamUsed, long totalRamUsed)
        {
            this.ManagedRamUsed = managedRamUsed;
            this.TotalRamUsed = totalRamUsed;
        }

        public long ManagedRamUsed { get; }
        public long TotalRamUsed { get; }
    }

    public interface IPerformanceMonitor
    {
        public void Report();

        public void Start();

        public void Join();

        public void Stop();

        /// <summary>
        ///     Raised when a simulator reports information about its
        ///     performance.
        /// </summary>
        public event EventHandler<SimulatorPerformanceArgs> OnSimulatorPerformanceAvailable;

    }
}
