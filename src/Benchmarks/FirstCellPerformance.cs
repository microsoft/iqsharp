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

    public class FirstCellPerformance
    {
        private IQSharpEngine? engine = null;
        private readonly IChannel nullChannel = new NullChannel();

        public FirstCellPerformance()
        {
        }

        [IterationSetup]
        public void CreateEngine()
        {
            engine = Startup.StartEngine().Result;
        }

        [IterationCleanup]
        public void CleanupEngine()
        {
            engine = null;
        }

        [Benchmark]
        public void CompileOne()
        {
            engine!.Execute(@"
                operation SayHello() : Unit {
                    Message(""Hi!"");
                }
            ", nullChannel);
        }

        [Benchmark]
        public void UseOneMagic()
        {
            engine!.Execute("%lsmagic", nullChannel);
        }
    }
}
