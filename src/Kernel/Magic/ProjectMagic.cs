// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Jupyter;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    ///     A magic command that can be used to add Q# project references to
    ///     the current IQ# session.
    /// </summary>
    public class ProjectMagic : AbstractMagic
    {
        private const string ParameterNameProjectFile = "__projectFile__";

        /// <summary>
        ///     Constructs a new magic command that adds Q# project references to
        ///     the current IQ# session.
        /// </summary>
        public ProjectMagic(IWorkspace workspace) : base(
            "project",
            new Documentation
            {
                Summary = "Provides the ability to view or add Q# project references.",
                Description = @"
                    This magic command allows for adding references to Q# projects to be compiled and loaded
                    into the current IQ# session.

                    The command accepts a single argument, which is the path to a .csproj file to be loaded.
                    The .csproj file must reference the Microsoft.Quantum.Sdk. The provided path may be either
                    an absolute path or a path relative to the current workspace root folder (usually the
                    folder containing the current .ipynb file). The project file will be added to the session
                    and then the workspace will be reloaded, which will automatically load any downstream
                    packages or projects referenced by the specified .csproj file and will recompile all
                    associated .qs source files.

                    If no argument is provided, the command simply returns the list of projects loaded in
                    the current IQ# session.
                ".Dedent(),
                Examples = new []
                {
                    @"
                        Add a reference to the `C:\Projects\MyProject.csproj` Q# project into the current IQ# session:
                        ```
                        In []: %project C:\Projects\MyProject.csproj
                        Out[]: Loading project C:\Projects\MyProject.csproj and dependencies...
                               <list of all loaded Q# project references>
                        ```
                    ".Dedent(),
                    
                    @"
                        View the list of all Q# project references that have been loaded into the current IQ# session:
                        ```
                        In []: %project
                        Out[]: <list of all loaded Q# project references>
                        ```
                    ".Dedent(),
                }
            })
        {
            this.Workspace = workspace;
        }

        /// <summary>
        ///     The workspace to which project references will be added.
        /// </summary>
        public IWorkspace Workspace { get; }

        /// <inheritdoc />
        public override ExecutionResult Run(string input, IChannel channel)
        {
            var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNameProjectFile);
            var projectFile = inputParameters.DecodeParameter<string>(ParameterNameProjectFile);

            if (!string.IsNullOrWhiteSpace(projectFile))
            {
                channel.Stdout($"Adding reference to project: {projectFile}");
                Workspace.AddProjectAsync(projectFile).Wait();
                WorkspaceMagic.Reload(Workspace, channel);
            }

            return Workspace
                .GetProjectsAsync()
                .Result
                .Select(project => project.ProjectFile)
                .Where(projectFile => !string.IsNullOrWhiteSpace(projectFile))
                .OrderBy(projectFile => projectFile)
                .ToArray()
                .ToExecutionResult();
        }
    }
}
