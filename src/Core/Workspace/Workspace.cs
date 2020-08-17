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
        /// This event is triggered whenever the workspace is reloaded.
        /// </summary>
        public event EventHandler<ReloadedEventArgs> Reloaded;

        /// <summary>
        /// This event is triggered whenever a project is loaded into the workspace.
        /// </summary>
        public event EventHandler<ProjectLoadedEventArgs> ProjectLoaded;

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

        private List<Project> UserAddedProjects { get; } = new List<Project>();

        /// <inheritdoc/>
        public IEnumerable<Project> Projects { get; set; } = Enumerable.Empty<Project>();

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
            var projects = new List<Project>() { Project.FromWorkspaceFolder(Root, CacheFolder, SkipAutoLoadProject) };
            projects.AddRange(UserAddedProjects);

            var projectsToResolve = projects.Distinct(new ProjectFileComparer()).ToList();
            while (projectsToResolve.Any())
            {
                try
                {
                    Logger?.LogInformation($"Looking for project references in .csproj files: {projectsToResolve.ToLogString()}");
                    projectsToResolve = projectsToResolve.SelectMany(project => project.ProjectReferences).ToList();

                    // Move any already-referenced projects to the end of the list, since other projects depend on them.
                    var alreadyReferencedProjects = projectsToResolve
                        .Where(project => projects.Contains(project, new ProjectFileComparer()));
                    if (alreadyReferencedProjects.Any())
                    {
                        projects = projects
                            .Except(alreadyReferencedProjects, new ProjectFileComparer())
                            .Concat(alreadyReferencedProjects)
                            .ToList();
                        projectsToResolve = projectsToResolve.Except(alreadyReferencedProjects).ToList();
                    }

                    // Skip projects that do not exist on disk.
                    var invalidProjects = projectsToResolve.Where(project => !File.Exists(project.ProjectFile));
                    if (invalidProjects.Any())
                    {
                        Logger?.LogError($"Skipping invalid project references: {invalidProjects.ToLogString()}");
                        projectsToResolve = projectsToResolve.Except(invalidProjects, new ProjectFileComparer()).ToList();
                    }

                    // Skip projects that do not reference Microsoft.Quantum.Sdk.
                    var nonSdkProjects = projectsToResolve.Where(project => !project.UsesQuantumSdk);
                    if (nonSdkProjects.Any())
                    {
                        Logger?.LogInformation($"Skipping project references that do not reference Microsoft.Quantum.Sdk: {nonSdkProjects.ToLogString()}");
                        projectsToResolve = projectsToResolve.Except(nonSdkProjects, new ProjectFileComparer()).ToList();
                    }

                    // Warn if any projects reference a Microsoft.Quantum.Sdk version that is different than
                    // the version of Microsoft.Quantum.Standard we have currently loaded.
                    if (GlobalReferences is References references)
                    {
                        const string standardPackageName = "Microsoft.Quantum.Standard";
                        var currentVersion = references
                            .Nugets
                            ?.Items
                            ?.Where(package => package.Id == standardPackageName)
                            .FirstOrDefault()
                            ?.Version
                            ?.ToString();
                        if (!string.IsNullOrEmpty(currentVersion))
                        {
                            foreach (var project in projectsToResolve.Where(project => !project.Sdk.EndsWith(currentVersion)))
                            {
                                Logger?.LogWarning(
                                    $"Project {project.ProjectFile} references {project.Sdk}, " +
                                    $"but IQ# is using version {currentVersion} of {standardPackageName}. " +
                                    $"Project will be compiled using version {currentVersion}.");
                            }
                        }
                    }

                    Logger?.LogInformation($"Adding project references: {projectsToResolve.ToLogString()}");
                    projects.AddRange(projectsToResolve);
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, $"Failed to resolve all project references: {e.Message}");
                    projectsToResolve = new List<Project>();
                }
            }

            // The list must now be reversed to reflect the order in which the projects must be built.
            projects.Reverse();
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

        private void LoadReferencedPackages(Action<string> statusCallback = null)
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
                            GlobalReferences.AddPackage(package, (newStatus) =>
                            {
                                statusCallback?.Invoke($"Adding package {package}: {newStatus}");
                            });
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

        /// <inheritdoc/>
        public void AddProject(string projectFile)
        {
            var fullProjectPath = Path.GetFullPath(projectFile, Root);
            if (!File.Exists(fullProjectPath))
            {
                throw new FileNotFoundException($"Project {fullProjectPath} not found. Please specify a path to a .csproj file.");
            }

            if (!fullProjectPath.EndsWith(".csproj"))
            {
                throw new InvalidOperationException("Please specify a path to a .csproj file.");
            }

            var project = Project.FromProjectFile(fullProjectPath, CacheFolder);
            if (!project.UsesQuantumSdk)
            {
                throw new InvalidOperationException("Please specify a project that references Microsoft.Quantum.Sdk.");
            }

            if (Projects.Contains(project, new ProjectFileComparer()))
            {
                throw new InvalidOperationException($"Project {fullProjectPath} has already been loaded in this session.");
            }

            UserAddedProjects.Add(project);
        }

        /// <summary>
        /// Reloads the workspace from disk.
        /// </summary>
        public void Reload(Action<string> statusCallback = null)
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
                LoadReferencedPackages(statusCallback);
                if (MonitorWorkspace)
                {
                    StartFileWatching();
                }

                ErrorMessages = new string[0];

                foreach (var project in Projects)
                {
                    var projectLoadDuration = Stopwatch.StartNew();

                    if (File.Exists(project.CacheDllPath)) { File.Delete(project.CacheDllPath); }

                    if (project.SourceFiles.Count() > 0)
                    {
                        Logger?.LogDebug($"{project.SourceFiles.Count()} found in project {project.ProjectFile}. Compiling.");
                        statusCallback?.Invoke(
                            string.IsNullOrWhiteSpace(project.ProjectFile)
                            ? "Compiling workspace"
                            : $"Compiling {project.ProjectFile}");

                        try
                        {
                            project.AssemblyInfo = Compiler.BuildFiles(
                                project.SourceFiles.ToArray(),
                                GlobalReferences.CompilerMetadata.WithAssemblies(Assemblies.ToArray()),
                                logger,
                                project.CacheDllPath);
                        }
                        catch (Exception e)
                        {
                            logger.LogError(
                                "IQS003",
                                $"Error compiling project {project.ProjectFile}: {e.Message}");
                            project.AssemblyInfo = new AssemblyInfo(null, null, null);
                        }

                        ErrorMessages = ErrorMessages.Concat(logger.Errors.ToArray());
                        errorIds.AddRange(logger.ErrorIds.ToArray());
                        fileCount += project.SourceFiles.Count();
                    }
                    else
                    {
                        Logger?.LogDebug($"No files found in project {project.ProjectFile}. Using empty workspace.");
                        project.AssemblyInfo = new AssemblyInfo(null, null, null);
                    }

                    if (!string.IsNullOrWhiteSpace(project.ProjectFile))
                    {
                        ProjectLoaded?.Invoke(this, new ProjectLoadedEventArgs(
                            new Uri(project.ProjectFile),
                            project.SourceFiles.Count(),
                            project.ProjectReferences.Count(),
                            project.PackageReferences.Count(),
                            projectLoadDuration.Elapsed));
                    }
                }

            }
            finally
            {
                duration.Stop();
                var status = this.HasErrors ? "error" : "ok";

                Logger?.LogInformation($"Reloading complete ({status}).");
                Reloaded?.Invoke(this, new ReloadedEventArgs(Root, status, fileCount, Projects.Count(), errorIds.ToArray(), duration.Elapsed));
            }
        }
    }
}
