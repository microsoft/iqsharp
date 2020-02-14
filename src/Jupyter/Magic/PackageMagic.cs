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
            var (name, _) = ParseInput(input);
            var status = new TaskStatus($"Adding package {name}");
            var statusUpdater = channel.DisplayUpdatable(status);
            void Update() => statusUpdater.Update(status);

            var task = RunAsync(name, channel, (newStatus) =>
            {
                status.Subtask = newStatus;
                Update();
            });
            using (Observable
                   .Interval(TimeSpan.FromSeconds(1))
                   .TakeUntil((idx) => task.IsCompleted)
                   .Do(idx => Update())
                   .Subscribe())
            {
                task.Wait();
            }
            status.Subtask = "done";
            status.IsCompleted = true;
            Update();
            return task.Result;
        }

        public async Task<ExecutionResult> RunAsync(string name, IChannel channel, Action<string> statusCallback)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                await References.AddPackage(name, statusCallback);
            }

            return References.Packages.ToArray().ToExecutionResult();
        }
    }
}
