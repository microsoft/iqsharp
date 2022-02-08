// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    ///     A magic symbol that provides access to a given workspace.
    /// </summary>
    public class WorkspaceMagic : AbstractMagic
    {
        private const string ParameterNameCommand = "__command__";

        /// <summary>
        ///      Given a workspace, constructs a new magic symbol to control
        ///      that workspace.
        /// </summary>
        public WorkspaceMagic(IWorkspace workspace, ILogger<WorkspaceMagic> logger) : base(
            "workspace", 
            new Microsoft.Jupyter.Core.Documentation
            {
                Summary = "Provides actions related to the current workspace.",
                Description = @"
                    This magic command allows for displaying and reloading the Q# operations and functions
                    defined within .qs files in the current folder.

                    If no parameters are provided, the command displays a list of Q# operations or functions
                    within .qs files in the current folder which are available
                    in the current IQ# session for use with magic commands such as `%simulate`
                    and `%estimate`.

                    The command will also output any errors encountered while compiling the .qs files
                    in the current folder.
                    
                    #### Optional parameters

                    - `reload`: Causes the IQ# kernel to recompile all .qs files in the current folder.
                ".Dedent(),
                Examples = new []
                {
                    @"
                        Display the list of Q# operations and functions available in the current folder:
                        ```
                        In []: %workspace
                        Out[]: <list of Q# operation and function names>
                        ```
                    ".Dedent(),
                    @"
                        Recompile the .qs files in the current folder:
                        ```
                        In []: %workspace reload
                        Out[]: <list of Q# operation and function names>
                        ```
                    ".Dedent(),
                }
            }, logger)
        {
            this.Workspace = workspace;
        }

        /// <summary>
        ///     The workspace controlled by this magic symbol.
        /// </summary>
        public IWorkspace Workspace { get; }

        /// <summary>
        /// Performs checks to verify if the Workspace is available and in a success (no errors) state.
        /// The method throws Exceptions if it finds it is not ready to execute.
        /// </summary>
        public void CheckIfReady()
        {
            if (Workspace == null)
            {
                throw new InvalidWorkspaceException($"Workspace is not ready. Try again.");
            }
            else if (Workspace.HasErrors)
            {
                throw new InvalidWorkspaceException(Workspace.ErrorMessages.ToArray());
            }
        }

        internal static void Reload(IWorkspace workspace, IChannel channel)
        {
            var status = new Jupyter.TaskStatus($"Reloading workspace");
            var statusUpdater = channel.DisplayUpdatable(status);
            void Update() => statusUpdater.Update(status);
            var task = Task.Run(() =>
            {
                workspace.Reload((newStatus) =>
                {
                    status.Subtask = newStatus;
                    Update();
                });
            });

            try
            {
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
            }
            catch (Exception)
            {
                status.Subtask = "error";
                status.IsCompleted = true;
                Update();
                throw;
            }
        }

        /// <inheritdoc />
        public override ExecutionResult Run(string input, IChannel channel)
        {
            var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNameCommand);
            var command = inputParameters.DecodeParameter<string>(ParameterNameCommand);

            if (string.IsNullOrWhiteSpace(command))
            {
                // if no command, just return the current state.
            }
            else if ("reload" == command)
            {
                Reload(Workspace, channel);
            }
            else
            {
                channel.Stderr($"Invalid action: {command}");
                return ExecuteStatus.Error.ToExecutionResult();
            }

            CheckIfReady();

            var names = Workspace?
                .Assemblies?
                .SelectMany(asm => asm.Operations)
                .Select(c => c.FullName)
                .OrderBy(name => name)
                .ToArray();
            return names.ToExecutionResult();
        }
    }
}
