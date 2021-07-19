// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Kernel;
using Microsoft.Quantum.IQSharp.Mocks;

namespace Microsoft.Quantum.IQSharp.Benchmarks
{

    public class StartupPerformance
    {
        private readonly IChannel nullChannel = new NullChannel();

        public StartupPerformance()
        {
        }

        [Benchmark]
        public void CreateEngine()
        {
            Startup.StartEngine().Wait();
        }
    }
}
