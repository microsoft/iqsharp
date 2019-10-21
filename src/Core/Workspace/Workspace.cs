// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Quantum.IQSharp.Common;

namespace Microsoft.Quantum.IQSharp
{

    /// <summary>
    /// A Workspace represents a folder in the host with .qs files
    /// that are compiled into an Assembly.
    /// </summary>
    public class Workspace : IWorkspace
    {
        public static readonly string SETTING_WORKSPACE = "Workspace";
        public static readonly string SETTING_CACHE_FOLDER = "CacheFolder";
        public static readonly string SETTING_MONITOR_WORKSPACE = "MonitorWorkspace";

        // We use this to keep track of file changes in the workspace and trigger a reload.
        private FileSystemWatcher Watcher;

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
        public Workspace(IRuntimeSettings config, ICompilerService compiler, IReferences references, ILogger<Workspace> logger)
        {
            Compiler = compiler;
            GlobalReferences = references;
            Logger = logger;

            if (config != null)
            {
                config.SettingSet += OnSettingChanged;
            }

            Root = config?[SETTING_WORKSPACE] ?? Directory.GetCurrentDirectory();
            CacheFolder = config?[SETTING_CACHE_FOLDER] ?? Path.Combine(Root, "obj");

            var monitor = config.GetOptional(SETTING_MONITOR_WORKSPACE);

            logger?.LogInformation(
                $"Starting IQ# Workspace:\n" +
                $"----------------\n" +
                $"Root: {Root}\n" +
                $"Cache folder:{CacheFolder}\n" +
                $"Monitoring changes: {monitor}\n" +
                $"User agent: {config?[RuntimeSettings.SETTING_USER_AGENT] ?? " <unknown> "}\n" +
                $"Hosting environment: {config?[RuntimeSettings.SETTING_HOSTING_ENV] ?? " <unknown> "}\n" +
                $"----------------");

            if (!LoadFromCache())
            {
                Reload();
            }

            if (monitor)
            {
                StartFileWatching();
            }
        }

        /// <summary>
        /// We monitor changes on .qs files and reload the Workspace on any events;
        /// </summary>
        private void StartFileWatching()
        {
            // Create a new FileSystemWatcher and set its properties.
            Watcher = new FileSystemWatcher(Root, "*.qs");
            Watcher.IncludeSubdirectories = true;
            Watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;


            // Add event handlers.
            Watcher.Changed += new FileSystemEventHandler(OnFilesChanged);
            Watcher.Created += new FileSystemEventHandler(OnFilesChanged);
            Watcher.Deleted += new FileSystemEventHandler(OnFilesChanged);
            Watcher.Renamed += new RenamedEventHandler(OnFilesRenamed);

            // Begin watching.
            Watcher.EnableRaisingEvents = true;
        }

        private void OnFilesChanged(object source, FileSystemEventArgs e) => Reload();

        private void OnFilesRenamed(object source, RenamedEventArgs e) => Reload();

        private void OnSettingChanged(object source, SettingSetEventArgs args)
        {
            if (string.Compare(args.Key, SETTING_WORKSPACE, StringComparison.OrdinalIgnoreCase) == 0)
            {
                // For security reasons, only change the Root folder
                // if it's within the current working directory and it exists.
                var wanted = args.Value;
                var fullPath = Path.GetFullPath(wanted);

                if (fullPath.StartsWith(Directory.GetCurrentDirectory()))
                {
                    if (Directory.Exists(fullPath))
                    {
                        Root = fullPath; 
                        CacheFolder = Path.Combine(Root, "obj");
                        Reload();

                        return;
                    }

                    Logger?.LogError($"Trying to set workspace root folder to a path that doesn't exist. Requested path: {fullPath}");
                }
                else
                {
                    Logger?.LogError($"Trying to set workspace root folder to a path outside the current working directory. Working directory: {Directory.GetCurrentDirectory()}. New path: {wanted}");
                }
            }
        }

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
        /// Compares the timestamp of the cache.dll with the timestamp of each of the .qs files
        /// and returns false if any of the .qs files is more recent.
        /// </summary>
        private bool IsCacheFresh()
        {
            if (!File.Exists(CacheDll)) return false;
            var last = File.GetLastWriteTime(CacheDll);

            foreach (var f in Directory.EnumerateFiles(Root, "*.qs", SearchOption.AllDirectories))
            {
                if (File.GetLastWriteTime(f) > last)
                {
                    Logger?.LogDebug($"Cache {CacheDll} busted by {f}.");
                    return false;
                }
            }

            return true;
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

                files = Directory.EnumerateFiles(Root, "*.qs", SearchOption.AllDirectories).ToArray();

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
