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

            public bool MonitorWorkspace { get; set; }

            /// <summary>
            /// Whether to skip automatically loading the .csproj from the workspace's root folder.
            /// </summary>
            public bool SkipAutoLoadProject { get; set; }
        }

        // We use this to keep track of file changes in the workspace and trigger a reload.
        private FileSystemWatcher[] Watchers = Array.Empty<FileSystemWatcher>();

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
        /// Gets or sets a value indicating whether to skip automatically loading the .csproj from the workspace's root folder.
        /// </summary>
        private bool SkipAutoLoadProject { get; set; }

        /// <summary>
        /// Gets the project files to be built for this Workspace.
        /// </summary>
        public IEnumerable<string> ProjectFiles
        {
            get
            {
                var projectFiles = Directory.EnumerateFiles(Root, "*.csproj", SearchOption.TopDirectoryOnly);
                if (SkipAutoLoadProject || projectFiles.Count() != 1) return Enumerable.Empty<string>();

                var projectFilesToResolve = projectFiles.ToArray();
                while (projectFilesToResolve.Any())
                {
                    try
                    {
                        Logger?.LogInformation($"Looking for project references in .csproj files: {string.Join(";", projectFilesToResolve)}");

                        projectFilesToResolve = projectFilesToResolve
                            .SelectMany(projectFile =>
                                XDocument.Load(projectFile)
                                    .XPathSelectElements("//ProjectReference")
                                    .Select(element => Path.Combine(
                                        Path.GetDirectoryName(projectFile),
                                        element.Attribute("Include").Value.Replace('\\', Path.DirectorySeparatorChar))))
                            .Where(projectFile => !projectFiles.Contains(projectFile))
                            .ToArray();

                        var missingFiles = projectFilesToResolve.Where(projectFile => !File.Exists(projectFile));
                        if (missingFiles.Any())
                        {
                            Logger?.LogError($"Skipping invalid project references: {string.Join(";", missingFiles)}");
                            projectFilesToResolve = projectFilesToResolve.Except(missingFiles).ToArray();
                        }

                        Logger?.LogInformation($"Adding project references: {string.Join(";", projectFilesToResolve)}");

                        projectFiles = projectFiles.Concat(projectFilesToResolve);
                    }
                    catch (Exception e)
                    {
                        Logger?.LogError(e, $"Failed to resolve all project references: {e.Message}");
                        projectFilesToResolve = Array.Empty<string>();
                    }
                }

                return projectFiles.Distinct();
            }
        }

        /// <summary>
        /// Gets the source folders containing source files to be built for this Workspace.
        /// </summary>
        private IEnumerable<string> SourceFolders =>
            ProjectFiles
                .Select(projectFile => Path.GetDirectoryName(projectFile))
                .Append(Root)
                .Distinct();

        /// <summary>
        /// Gets the source files to be built for this Workspace.
        /// </summary>
        public IEnumerable<string> SourceFiles =>
            SourceFolders.SelectMany(folder => Directory.EnumerateFiles(folder, "*.qs", SearchOption.TopDirectoryOnly));

        /// <summary>
        /// Information about the assembly built from this Workspace.
        /// </summary>
        public AssemblyInfo AssemblyInfo { get; set; }

        /// <summary>
        /// The compilation errors, if any.
        /// </summary>
        public IEnumerable<string> ErrorMessages { get; set; }

        /// <summary>
        /// If any of the files in the workspace had any compilation errors.
        /// </summary>
        public bool HasErrors => ErrorMessages == null || ErrorMessages.Any();

        /// <summary>
        /// The folder where the assembly is permanently saved for cache.
        /// </summary>
        public string CacheFolder { get; set; }

        /// <summary>
        /// The full qualified file name of the assembly built from this Workspace
        /// </summary>
        public string CacheDll => Path.Combine(CacheFolder, "__ws__.dll");

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

            var monitor = config?.Value.MonitorWorkspace == true;

            logger?.LogInformation($"Starting IQ# Workspace:\n----------------\nRoot: {Root}\nCache folder:{CacheFolder}\nMonitoring changes: {monitor}\nUser agent: {metadata?.UserAgent ?? "<unknown>"}\nHosting environment: {metadata?.HostingEnvironment ?? "<unknown>"}\n----------------");

            if (!LoadFromCache())
            {
                Reload();
            }
            else
            {
                LoadReferencedPackages();
            }

            if (monitor)
            {
                StartFileWatching();
            }

            eventService?.TriggerServiceInitialized<IWorkspace>(this);
        }

        /// <summary>
        /// We monitor changes on .qs files and reload the Workspace on any events;
        /// </summary>
        private void StartFileWatching()
        {
            // Create a new FileSystemWatcher for each source folder and set its properties.
            Watchers = SourceFolders.Select(folder =>  new FileSystemWatcher(folder, "*.qs"))
                .Concat(SourceFolders.Select(folder => new FileSystemWatcher(folder, "*.csproj")))
                .ToArray();

            foreach (var watcher in Watchers)
            {
                watcher.IncludeSubdirectories = false;
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;

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
                Logger?.LogDebug($"Loading workspace from cache assembly: {CacheDll}.");
                var data = File.ReadAllBytes(CacheDll);
                var assm = System.Reflection.Assembly.Load(data);
                AssemblyInfo = new AssemblyInfo(assm, CacheDll, null);
                ErrorMessages = new string[0];

                return true;
            }

            return false;
        }

        /// <summary>
        /// Compares the timestamp of the cache.dll with the timestamp of each of the .qs and .csproj files
        /// and returns false if any of the .qs or .csproj files is more recent.
        /// </summary>
        private bool IsCacheFresh()
        {
            if (!File.Exists(CacheDll)) return false;
            var last = File.GetLastWriteTime(CacheDll);

            foreach (var f in SourceFiles.Concat(ProjectFiles))
            {
                if (File.GetLastWriteTime(f) > last)
                {
                    Logger?.LogDebug($"Cache {CacheDll} busted by {f}.");
                    return false;
                }
            }

            return true;
        }

        private void LoadReferencedPackages()
        {
            foreach (var projectFile in ProjectFiles)
            {
                try
                {
                    var packageReferences = XDocument.Load(projectFile)
                        .XPathSelectElements("//PackageReference")
                        .Select(element => new PackageReference(
                            new PackageIdentity(
                                id: element.Attribute("Include").Value,
                                version: new NuGetVersion(element.Attribute("Version").Value)),
                            NuGetFramework.AnyFramework));

                    foreach (var packageReference in packageReferences)
                    {
                        var package = $"{packageReference.PackageIdentity.Id}::{packageReference.PackageIdentity.Version}";
                        try
                        {
                            Logger?.LogInformation($"Loading package {package} for project {projectFile}.");
                            GlobalReferences.AddPackage(package);
                        }
                        catch (Exception e)
                        {
                            Logger?.LogError(e, $"Failed to load package {package} for project {projectFile}: {e.Message}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, $"Failure loading packages for project {projectFile}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Reloads the workspace from disk.
        /// </summary>
        public void Reload()
        {
            var duration = Stopwatch.StartNew();
            var files = new string[0];
            var errorIds = new string[0];

            try
            {
                Logger?.LogInformation($"Reloading workspace at {Root}.");

                // Create a new logger for this compilation:
                var logger = new QSharpLogger(Logger);

                if (File.Exists(CacheDll)) { File.Delete(CacheDll); }

                LoadReferencedPackages();

                files = SourceFiles.ToArray();

                if (files.Length > 0)
                {
                    Logger?.LogDebug($"{files.Length} found in workspace. Compiling.");
                    AssemblyInfo = Compiler.BuildFiles(files, GlobalReferences.CompilerMetadata, logger, CacheDll);
                    ErrorMessages = logger.Errors.ToArray();
                    errorIds = logger.ErrorIds.ToArray();
                }
                else
                {
                    Logger?.LogDebug($"No files found in Workspace. Using empty workspace.");
                    AssemblyInfo = new AssemblyInfo(null, null, null);
                    ErrorMessages = new string[0];
                }

            }
            finally
            {
                duration.Stop();
                var status = this.HasErrors ? "error" : "ok";

                Logger?.LogInformation($"Reloading complete ({status}).");
                Reloaded?.Invoke(this, new ReloadedEventArgs(Root, status, files.Length, errorIds, duration.Elapsed));
            }
        }
    }
}
