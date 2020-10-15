// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Quantum.IQSharp.Common;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// List of arguments that are part of the Reloaded event.
    /// </summary>
    public class ReloadedEventArgs : EventArgs
    {
        public ReloadedEventArgs(string workspace, string status, int fileCount, int projectCount, string[] errors, TimeSpan duration)
        {
            this.Workspace = workspace;
            this.Status = status;
            this.FileCount = fileCount;
            this.ProjectCount = projectCount;
            this.Errors = errors;
            this.Duration = duration;
        }

        /// <summary>
        /// The name of IQ#'s workspace folder
        /// </summary>
        public string Workspace { get; }

        /// <summary>
        /// The workspace status. Can be "success" or "error"
        /// </summary>
        public string Status { get; }

        /// <summary>
        /// The number of files used for compilation.
        /// </summary>
        public int FileCount { get; }

        /// <summary>
        /// The number of projects used for compilation.
        /// </summary>
        public int ProjectCount { get; }

        /// <summary>
        /// The list of error ids reported by the Q# parser (if any).
        /// </summary>
        public string[] Errors { get; }

        /// <summary>
        /// The total time the reload operation took.
        /// </summary>
        public TimeSpan Duration { get; }
    }

    /// <summary>
    /// A Workspace represents a folder in the host with .qs files
    /// that are compiled into an Assembly.
    /// </summary>
    public interface IWorkspace
    {
        /// <summary>
        /// This event is triggered whenever the workspace is reloaded.
        /// </summary>
        event EventHandler<ReloadedEventArgs> Reloaded;

        /// <summary>
        /// This event is triggered whenever a project is loaded into the workspace.
        /// </summary>
        event EventHandler<ProjectLoadedEventArgs> ProjectLoaded;

        /// <summary>
        /// The root folder.
        /// </summary>
        string Root { get; }

        /// <summary>
        /// The folder where the assembly is permanently saved for cache.
        /// </summary>
        string CacheFolder { get; }

        /// <summary>
        /// Information of all assemblies built from this workspace that have
        /// already been fully compiled and are available. This may not be the complete
        /// list of assemblies for the workspace if called during workspace initialization.
        /// If the full list is required, use <see cref="GetAssembliesAsync"/>.
        /// </summary>
        IEnumerable<AssemblyInfo> AvailableAssemblies { get; }

        /// <summary>
        /// Gets the projects to be built for this Workspace. The order of the enumeration
        /// indicates the order in which the projects should be built, i.e., the first
        /// project in the enumeration should be built first.
        /// </summary>
        Task<IEnumerable<Project>> GetProjectsAsync();

        /// <summary>
        /// Attempt to add a Q# project reference to this workspace. This does not trigger
        /// recompilation of the workspace. Call <see cref="Reload()"/> to recompile.
        /// </summary>
        /// <param name="projectFile">
        /// The path to the project file.
        /// Must be either absolute or relative to <see cref="Root"/>.
        /// </param>
        /// <remarks>
        /// This will also cause any Q# projects referenced by the specified project file
        /// to be added implicitly to the list of project references in the workspace.
        /// All such projects will be recompiled when the workspace is reloaded.
        /// </remarks>
        Task AddProjectAsync(string projectFile);

        /// <summary>
        /// Gets the source files to be built for this Workspace.
        /// </summary>
        Task<IEnumerable<string>> GetSourceFilesAsync();

        /// <summary>
        /// Information of all assemblies built from this Workspace.
        /// </summary>
        Task<IEnumerable<AssemblyInfo>> GetAssembliesAsync();

        /// <summary>
        /// The compilation errors, if any.
        /// </summary>
        Task<IEnumerable<string>> GetErrorMessagesAsync();

        /// <summary>
        /// If any of the files in the workspace had any compilation errors.
        /// </summary>
        Task<bool> GetHasErrorsAsync();

        /// <summary>
        /// Triggers the workspace to be reloaded from disk.
        /// </summary>
        Task ReloadAsync(Action<string> statusCallback = null);
    }
}
