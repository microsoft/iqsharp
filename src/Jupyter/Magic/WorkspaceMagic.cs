// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;

using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Jupyter;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    public class WorkspaceMagic : AbstractMagic
    {
        public WorkspaceMagic(IWorkspace workspace) : base(
            "workspace", 
            new Documentation {
                Summary = "Returns a list of all operations and functions defined in the current session, either interactively or loaded from the current workspace."
            })
        {
            this.Workspace = workspace;
        }

        public IWorkspace Workspace { get; }

        /// <summary>
        /// Performs checks to verify if the Workspace is avaialble and in a success (no errors) state.
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

        public override ExecutionResult Run(string input, IChannel channel)
        {
            var (command, _) = ParseInput(input);

            if (string.IsNullOrWhiteSpace(command))
            {
                // if no command, just return the current state.
            }
            else if ("reload" == command)
            {
                Workspace.Reload();
            }
            else
            {
                channel.Stderr($"Invalid action: {command}");
                return ExecuteStatus.Error.ToExecutionResult();
            }

            CheckIfReady();

            var names = Workspace?.AssemblyInfo?.Operations?
                .Select(c => c.FullName)
                .OrderBy(name => name)
                .ToArray();
            return names.ToExecutionResult();
        }
    }
}
