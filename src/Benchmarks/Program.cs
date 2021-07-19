// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Running;

namespace Microsoft.Quantum.IQSharp.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var envs = System.Environment.GetEnvironmentVariables();
            var config = DefaultConfig
                .Instance
                .AddExporter(JsonExporter.FullCompressed)
                .AddExporter(CsvExporter.Default)
                .AddColumn(new TagColumn("Commit", _ => System.Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "<unknown>"));
            
            BenchmarkRunner.Run<FirstCellPerformance>(config);
            BenchmarkRunner.Run<WarmPerformance>(config);
            BenchmarkRunner.Run<StartupPerformance>(config);
        }
    }
}
