﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

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
        /// Gets the projects to be built for this Workspace. The order of the enumeration
        /// indicates the order in which the projects should be built, i.e., the first
        /// project in the enumeration should be built first.
        /// </summary>
        public IEnumerable<Project> Projects { get; set; }

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
        public void AddProject(string projectFile);

        /// <summary>
        /// Gets the source files to be built for this Workspace.
        /// </summary>
        public IEnumerable<string> SourceFiles { get; }

        /// <summary>
        /// The folder where the assembly is permanently saved for cache.
        /// </summary>
        string CacheFolder { get; }

        /// <summary>
        /// Information of the assembly built from this Workspace.
        /// </summary>
        /// <remarks>
        /// This does NOT include assemblies built from any project references,
        /// and it will be <c>null</c> in the case that the assemblies are
        /// built from .csproj files.
        /// To get all assembly information, use the <see cref="Assemblies"/>
        /// property.
        /// </remarks>
        AssemblyInfo AssemblyInfo { get; }

        /// <summary>
        /// Information of all assemblies built from this Workspace.
        /// </summary>
        public IEnumerable<AssemblyInfo> Assemblies { get; }

        /// <summary>
        /// The compilation errors, if any.
        /// </summary>
        IEnumerable<string> ErrorMessages { get; }

        /// <summary>
        /// If any of the files in the workspace had any compilation errors.
        /// </summary>
        bool HasErrors { get; }

        /// <summary>
        /// Triggers the workspace to be reloaded from disk.
        /// </summary>
        void Reload(Action<string> statusCallback = null);
    }
}
