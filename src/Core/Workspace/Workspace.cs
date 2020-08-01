// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Quantum.IQSharp.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Microsoft.Quantum.IQSharp
{

    /// <summary>
    /// A Workspace represents a folder in the host with .qs files
    /// that are compiled into an Assembly.
    /// </summary>
    public class Workspace : IWorkspace
    {
        /// <summary>
        /// Settings that can be configured via command line or parameters.
        /// </summary>
        public class Settings
        {
            private string _workspace;
            private string _cacheFolder;

            /// <summary>
            /// The Workspace's root folder
            /// </summary>
            public string Workspace
            {
                get => _workspace ?? Directory.GetCurrentDirectory();
                set { _workspace = value; }
            }

            /// <summary>
            /// The folder where the assembly is permanently saved for cache.
            /// </summary>
            public string CacheFolder
            {
                get => _cacheFolder ?? Path.Combine(Workspace, "obj");
                set { _cacheFolder = value; }
            }

            /// <summary>
            /// Whether to monitor the file system for changes and automatically reload the workspace.
            /// </summary>
            public bool MonitorWorkspace { get; set; }

            /// <summary>
            /// Whether to skip automatically loading the .csproj from the workspace's root folder.
            /// </summary>
            public bool SkipAutoLoadProject { get; set; }
        }

        /// <summary>
        /// This event is triggered when ever the workspace is reloaded.
        /// </summary>
        public event EventHandler<ReloadedEventArgs> Reloaded;

        /// <summary>
        /// Logger instance used for .net core logging.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// All the references used to compile the Assembly of this workspace.
        /// </summary>
        public IReferences GlobalReferences { get; }

        /// <summary>
        /// The service that takes care of compiling code.
        /// </summary>
        public ICompilerService Compiler { get; }

        /// <summary>
        /// The root folder.
        /// </summary>
        public string Root { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to monitor the file system for
        /// changes and automatically reload the workspace.
        /// </summary>
        private bool MonitorWorkspace { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to skip automatically loading
        /// the .csproj from the workspace's root folder.
        /// </summary>
        private bool SkipAutoLoadProject { get; set; }

        /// <summary>
        /// Gets the projects to be built for this Workspace.
        /// </summary>
        public IEnumerable<Project> Projects { get; set; }

        /// <summary>
        /// Gets the source files to be built for this Workspace.
        /// </summary>
        public IEnumerable<string> SourceFiles => Projects.SelectMany(p => p.SourceFiles).Distinct();

        /// <inheritdoc/>
        public AssemblyInfo AssemblyInfo =>
            Projects
                .Where(p => string.IsNullOrEmpty(p.ProjectFile))
                .Select(p => p.AssemblyInfo)
                .FirstOrDefault();

        /// <inheritdoc/>
        public IEnumerable<AssemblyInfo> Assemblies =>
            Projects.Select(p => p.AssemblyInfo).Where(asm => asm != null);

        /// <summary>
        /// The compilation errors, if any.
        /// </summary>
        public IEnumerable<string> ErrorMessages { get; set; }

        /// <summary>
        /// If any of the files in the workspace had any compilation errors.
        /// </summary>
        public bool HasErrors => ErrorMessages == null || ErrorMessages.Any();

        /// <summary>
        /// The folder where the project assemblies are permanently saved for cache.
        /// </summary>
        public string CacheFolder { get; set; }

        /// <summary>
        /// Main constructor that accepts ILogger and IReferences as dependencies.
        /// </summary>
        /// <param name="logger">Used to log messages to the console.</param>
        /// <param name="references">List of references to use to compile the workspace.</param>
        public Workspace(
            IOptions<Settings> config, 
            ICompilerService compiler, 
            IReferences references, 
            ILogger<Workspace> logger, 
            IMetadataController metadata,
            IEventService eventService)
        {
            Compiler = compiler;
            GlobalReferences = references;
            Logger = logger;

            Root = config?.Value.Workspace;
            CacheFolder = config?.Value.CacheFolder;
            SkipAutoLoadProject = config?.Value.SkipAutoLoadProject ?? false;
            MonitorWorkspace = config?.Value.MonitorWorkspace ?? false;

            logger?.LogInformation($"Starting IQ# Workspace:\n----------------\nRoot: {Root}\nCache folder:{CacheFolder}\nMonitoring changes: {MonitorWorkspace}\nUser agent: {metadata?.UserAgent ?? "<unknown>"}\nHosting environment: {metadata?.HostingEnvironment ?? "<unknown>"}\n----------------");

            ResolveProjectReferences();

            if (!LoadFromCache())
            {
                Reload();
            }
            else
            {
                LoadReferencedPackages();
                if (MonitorWorkspace)
                {
                    StartFileWatching();
                }
            }

            eventService?.TriggerServiceInitialized<IWorkspace>(this);
        }

        private void ResolveProjectReferences()
        {
            var rootProject = Project.FromWorkspaceFolder(Root, CacheFolder, SkipAutoLoadProject);
            var projects = new List<Project>() { rootProject };

            var projectsToResolve = projects.ToList();
            while (projectsToResolve.Any())
            {
                try
                {
                    Logger?.LogInformation($"Looking for project references in .csproj files: {string.Join(";", projectsToResolve.Select(p => p.ProjectFile))}");
                    projectsToResolve = projectsToResolve
                        .SelectMany(project => project.ProjectReferences)
                        .Where(project => !projects.Select(p => p.ProjectFile).Contains(project.ProjectFile))
                        .ToList();

                    var invalidProjects = projectsToResolve.Where(project => !File.Exists(project.ProjectFile));
                    if (invalidProjects.Any())
                    {
                        Logger?.LogError($"Skipping invalid project references: {string.Join(";", invalidProjects.Select(p => p.ProjectFile))}");
                        projectsToResolve = projectsToResolve.Except(invalidProjects).ToList();
                    }

                    Logger?.LogInformation($"Adding project references: {string.Join(";", projectsToResolve.Select(p => p.ProjectFile))}");
                    projects.AddRange(projectsToResolve);
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, $"Failed to resolve all project references: {e.Message}");
                    projectsToResolve = new List<Project>();
                }
            }

            Projects = projects;
        }

        /// <summary>
        /// We monitor changes on .qs files and reload the Workspace on any events;
        /// </summary>
        private void StartFileWatching()
        {
            foreach (var watcher in Projects.SelectMany(p => p.Watchers))
            {
                // Add event handlers.
                watcher.Changed += new FileSystemEventHandler(OnFilesChanged);
                watcher.Created += new FileSystemEventHandler(OnFilesChanged);
                watcher.Deleted += new FileSystemEventHandler(OnFilesChanged);
                watcher.Renamed += new RenamedEventHandler(OnFilesRenamed);

                // Begin watching.
                watcher.EnableRaisingEvents = true;
            }
        }

        private void OnFilesChanged(object source, FileSystemEventArgs e) => Reload();

        private void OnFilesRenamed(object source, RenamedEventArgs e) => Reload();

        /// <summary>
        /// Tries to load the Workspace's information from cache. 
        /// Returns true on success, false if no cache information is available of the cache is not fresh.
        /// </summary>
        private bool LoadFromCache()
        {
            var dir = CacheFolder;
            if (!Directory.Exists(dir))
            {
                Logger?.LogDebug($"Creating cache folder {CacheFolder}.");
                Directory.CreateDirectory(dir);
            }

            if (IsCacheFresh())
            {
                Logger?.LogDebug($"Loading workspace from cached assemblies.");
                foreach (var project in Projects)
                {
                    Logger?.LogDebug($"Loading cache assembly: {project.CacheDllPath}.");
                    var data = File.ReadAllBytes(project.CacheDllPath);
                    var assm = System.Reflection.Assembly.Load(data);
                    project.AssemblyInfo = new AssemblyInfo(assm, project.CacheDllPath, null);
                }

                ErrorMessages = new string[0];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Compares the timestamp of the cache DLLs for each project with the timestamp of each of the
        /// .qs and .csproj files and returns false if any of the .qs or .csproj files is more recent.
        /// </summary>
        private bool IsCacheFresh()
        {
            foreach (var project in Projects)
            {
                if (!File.Exists(project.CacheDllPath))
                {
                    Logger?.LogDebug($"Cache {project.CacheDllPath} does not exist.");
                    return false;
                }

                var last = File.GetLastWriteTime(project.CacheDllPath);
                foreach (var f in project.SourceFiles.Append(project.ProjectFile))
                {
                    if (!string.IsNullOrEmpty(f) && File.GetLastWriteTime(f) > last)
                    {
                        Logger?.LogDebug($"Cache {project.CacheDllPath} busted by {f}.");
                        return false;
                    }
                }
            }

            return true;
        }

        private void LoadReferencedPackages()
        {
            foreach (var project in Projects)
            {
                try
                {
                    foreach (var packageReference in project.PackageReferences)
                    {
                        var package = $"{packageReference.PackageIdentity.Id}::{packageReference.PackageIdentity.Version}";
                        try
                        {
                            Logger?.LogInformation($"Loading package {package} for project {project.ProjectFile}.");
                            GlobalReferences.AddPackage(package);
                        }
                        catch (Exception e)
                        {
                            Logger?.LogError(e, $"Failed to load package {package} for project {project.ProjectFile}: {e.Message}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, $"Failure loading packages for project {project.ProjectFile}: {e.Message}");
                }
            }
        }

        public IEnumerable<AssemblyInfo> BuildAssemblies(QSharpLogger logger, CompilerMetadata compilerMetadata, string prefix, string executionTarget) =>
            Projects
                .Where(p => p.SourceFiles.Any())
                .Select(project => Compiler.BuildFiles(
                    project.SourceFiles.ToArray(), compilerMetadata, logger, $"__{prefix}{project.CacheDllName}", executionTarget));

        /// <summary>
        /// Reloads the workspace from disk.
        /// </summary>
        public void Reload()
        {
            var duration = Stopwatch.StartNew();
            var fileCount = 0;
            var errorIds = new List<string>();
            ErrorMessages = new List<string>();

            try
            {
                Logger?.LogInformation($"Reloading workspace at {Root}.");

                foreach (var project in Projects)
                {
                    if (File.Exists(project.CacheDllPath)) { File.Delete(project.CacheDllPath); }
                    foreach (var watcher in project.Watchers)
                    {
                        watcher.EnableRaisingEvents = false;
                        watcher.Dispose();
                    }
                }
                Projects = Enumerable.Empty<Project>();

                // Create a new logger for this compilation:
                var logger = new QSharpLogger(Logger);

                ResolveProjectReferences();
                LoadReferencedPackages();
                if (MonitorWorkspace)
                {
                    StartFileWatching();
                }

                ErrorMessages = new string[0];

                foreach (var project in Projects)
                {
                    if (File.Exists(project.CacheDllPath)) { File.Delete(project.CacheDllPath); }

                    if (project.SourceFiles.Count() > 0)
                    {
                        Logger?.LogDebug($"{project.SourceFiles.Count()} found in project {project.ProjectFile}. Compiling.");
                        project.AssemblyInfo = Compiler.BuildFiles(
                            project.SourceFiles.ToArray(),
                            GlobalReferences.CompilerMetadata,
                            logger,
                            project.CacheDllPath);

                        ErrorMessages = ErrorMessages.Concat(logger.Errors.ToArray());
                        errorIds.AddRange(logger.ErrorIds.ToArray());
                        fileCount += project.SourceFiles.Count();
                    }
                    else
                    {
                        Logger?.LogDebug($"No files found in project {project.ProjectFile}. Using empty workspace.");
                        project.AssemblyInfo = new AssemblyInfo(null, null, null);
                    }
                }

            }
            finally
            {
                duration.Stop();
                var status = this.HasErrors ? "error" : "ok";

                Logger?.LogInformation($"Reloading complete ({status}).");
                Reloaded?.Invoke(this, new ReloadedEventArgs(Root, status, fileCount, errorIds.ToArray(), duration.Elapsed));
            }
        }
    }
}
