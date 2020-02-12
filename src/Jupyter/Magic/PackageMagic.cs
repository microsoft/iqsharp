// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Jupyter;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    public class PackageMagic : AbstractMagic
    {
        public PackageMagic(IReferences references) : base(
            "package", 
            new Documentation {
                Summary = "Provides the ability to load a Nuget package. The package must be available on the list of nuget sources, typically this includes nuget.org"
            })
        {
            this.References = references;
        }

        public IReferences References { get; }

        public override ExecutionResult Run(string input, IChannel channel)
        {
            var task = RunAsync(input, channel);
            var message = "Adding...";
            var status = channel.DisplayUpdatable(message);
            Observable
                .Interval(TimeSpan.FromSeconds(1))
                .TakeUntil((idx) => task.IsCompleted)
                .Do(idx =>
                {
                    message += ".";
                    status.Update(message);
                })
                .Subscribe();
            return task.Result;
        }

        public async Task<ExecutionResult> RunAsync(string input, IChannel channel)
        {
            var (name, _) = ParseInput(input);

            if (!string.IsNullOrWhiteSpace(name))
            {
                await References.AddPackage(name);
            }

            return References.Packages.ToArray().ToExecutionResult();
        }
    }
}
