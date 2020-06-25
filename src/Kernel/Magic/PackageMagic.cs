// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Reactive.Linq;
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
                Summary = "Provides the ability to load a NuGet package.",
                Description = @"
                    This magic command allows for loading a NuGet package into the current IQ# kernel process.
                    The package must be available on the system's list of NuGet sources, which typically includes nuget.org.
                    Functionality such as magic commands and result encoders defined in the loaded package will
                    automatically become available for use in the current session.

                    The package can be specified by name only, or by name and version (using `name::version` syntax).

                    If no version is specified:

                    - For packages that are part of the Microsoft Quantum Development Kit, IQ# will attempt to
                    obtain the version of the package that matches the current IQ# version.
                    - For other packages, IQ# will attempt to obtain the most recent version of the package.
                ".Dedent(),
                Examples = new []
                {
                    @"
                        Load the `Microsoft.Quantum.MachineLearning` package into the current IQ# session:
                        ```
                        In []: %package Microsoft.Quantum.MachineLearning
                        Out[]: Adding package Microsoft.Quantum.MachineLearning: done!
                               <list of all loaded packages and versions>
                        ```
                    ".Dedent(),
                    
                    @"
                        Load a specific version of the `Microsoft.Quantum.Katas` package into the current IQ# session:
                        ```
                        In []: %package Microsoft.Quantum.Katas::0.11.2006.403
                        Out[]: Adding package Microsoft.Quantum.Katas::0.11.2006.403: done!
                               <list of all loaded packages and versions>
                        ```
                    ".Dedent(),
                    
                    @"
                        View the list of all packages that have been loaded into the current IQ# session:
                        ```
                        In []: %package
                        Out[]: <list of all loaded packages and versions>
                        ```
                    ".Dedent(),
                }
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
        public override ExecutionResult Run(string input, IChannel channel)
        {
            var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNamePackageName);
            var name = inputParameters.DecodeParameter<string>(ParameterNamePackageName);
            var status = new Jupyter.TaskStatus($"Adding package {name}");
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

        /// <summary>
        ///     Adds a package given a string representing its name and returns
        ///     a task that can be awaited on for the completion of the package
        ///     download.
        /// </summary>
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
