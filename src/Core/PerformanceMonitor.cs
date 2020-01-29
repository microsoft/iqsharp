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
    public class PerformanceMonitor
    {

        private readonly ILogger<PerformanceMonitor> Logger;
        private bool alive = false;
        private Thread? thread = null;

        public PerformanceMonitor(
            ILogger<PerformanceMonitor> logger
        )
        {
            Logger = logger;
        }

        public void Report() =>
            Logger.LogInformation(
                "Estimated RAM usage:" +
                "\n\tManaged: {Managed} bytes" +
                "\n\tTotal:   {Total} bytes",
                GC.GetTotalMemory(forceFullCollection: false),
                Process.GetCurrentProcess().WorkingSet64
            );

        public void Start()
        {
            alive = true;
            thread = new Thread(EventLoop);
            thread.Start();
        }

        public void Join() => thread?.Join();

        public void Stop()
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

    }
}
