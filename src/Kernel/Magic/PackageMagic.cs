// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.IQSharp.Kernel;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    ///     A magic command that can be used to add new NuGet packages to the
    ///     current IQ# session.
    /// </summary>
    public class PackageMagic : AbstractMagic
    {
        private const string ParameterNamePackageName = "__packageName__";

        /// <summary>
        ///     Constructs a new magic command that adds package references to
        ///     a given references collection.
        /// </summary>
        public PackageMagic(IReferences references) : base(
            "package",
            new Documentation {
                Summary = "Provides the ability to load a Nuget package. The package must be available on the list of nuget sources, typically this includes nuget.org"
            })
        {
            this.References = references;
        }

        /// <summary>
        ///     The references collection that this magic command adds package
        ///     references to.
        /// </summary>
        public IReferences References { get; }

        /// <inheritdoc />
        public override ExecutionResult Run(string input, IChannel channel, CancellationToken cancellationToken)
        {
            var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNamePackageName);
            var name = inputParameters.DecodeParameter<string>(ParameterNamePackageName);
            var status = new Jupyter.TaskStatus($"Adding package {name}");
            var statusUpdater = channel.DisplayUpdatable(status);
            void Update() => statusUpdater.Update(status);

            var task = RunAsync(name, channel, cancellationToken, (newStatus) =>
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

        /// <summary>
        ///     Adds a package given a string representing its name and returns
        ///     a task that can be awaited on for the completion of the package
        ///     download.
        /// </summary>
        public async Task<ExecutionResult> RunAsync(string name, IChannel channel, CancellationToken cancellationToken, Action<string> statusCallback)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                await References.AddPackage(name, statusCallback);
            }

            return References.Packages.ToArray().ToExecutionResult();
        }
    }
}
