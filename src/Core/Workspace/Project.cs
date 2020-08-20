// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Extensions.Logging;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Microsoft.Quantum.IQSharp
{
    internal static class ProjectExtensions
    {
        public static string ToLogString(this IEnumerable<Project> projects) =>
            string.Join(";", projects.Select(p => p.ProjectFile));
    }

    internal class ProjectFileComparer : EqualityComparer<Project>
    {
        private readonly StringComparison StringComparison =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        public override bool Equals(Project p1, Project p2) =>
            string.Equals(p1.ProjectFile, p2.ProjectFile, StringComparison);

        public override int GetHashCode(Project project) =>
            project.ProjectFile.GetHashCode(StringComparison);
    }

    /// <summary>
    /// List of arguments that are part of the project loaded event.
    /// </summary>
    public class ProjectLoadedEventArgs : EventArgs
    {
        public ProjectLoadedEventArgs(Uri? projectUri, int sourceFileCount, int projectReferenceCount, int packageReferenceCount, bool userAdded, TimeSpan duration)
        {
            this.ProjectUri = projectUri;
            this.SourceFileCount = sourceFileCount;
            this.ProjectReferenceCount = projectReferenceCount;
            this.PackageReferenceCount = packageReferenceCount;
            this.UserAdded = userAdded;
            this.Duration = duration;
        }

        /// <summary>
        /// The location of the project file.
        /// </summary>
        public Uri? ProjectUri { get; }

        /// <summary>
        /// The number of source files to be compiled for this project.
        /// </summary>
        public int SourceFileCount { get; }

        /// <summary>
        /// The number of project references identified for this project.
        /// </summary>
        public int ProjectReferenceCount { get; }

        /// <summary>
        /// The number of package references identified for this project.
        /// </summary>
        public int PackageReferenceCount { get; }

        /// <summary>
        /// Whether this project was explicitly added by a user vs. loaded implicitly.
        /// </summary>
        public bool UserAdded { get; }

        /// <summary>
        /// The total time the project load operation took.
        /// </summary>
        public TimeSpan Duration { get; }
    }

    /// <summary>
    /// Represents a Q# project referenced by a <see cref="Workspace"/>.
    /// 
    /// May be associated with a .csproj file on disk, in which case it will be associated
    /// with all .qs source files in the corresponding folder or subfolders.
    /// 
    /// If not associated with a .csproj file, it is assumed to be the default
    /// project for the workspace, which will be associated with the .qs files in the
    /// root folder of the workspace (but not subfolders).
    /// </summary>
    public class Project
    {
        public readonly string ProjectFile;
        internal readonly string RootFolder;
        internal readonly string CacheFolder;
        internal readonly IEnumerable<FileSystemWatcher> Watchers;

        /// <summary>
        /// Creates and returns <see cref="Project"/> objects corresponding to each
        /// <c>ProjectReference</c> element in the .csproj referenced by <see cref="ProjectFile"/>.
        /// </summary>
        internal IEnumerable<Project> ProjectReferences =>
            string.IsNullOrEmpty(ProjectFile)
            ? Enumerable.Empty<Project>()
            : XDocument.Load(ProjectFile)
                .XPathSelectElements("//ProjectReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(include => !string.IsNullOrEmpty(include))
                .Select(include => Path.GetFullPath(
                    include!.Replace('\\', Path.DirectorySeparatorChar),
                    Path.GetDirectoryName(ProjectFile)))
                .Select(projectFile => Project.FromProjectFile(projectFile, CacheFolder));

        /// <summary>
        /// Creates and returns <see cref="PackageReference"/> objects corresponding to each
        /// <c>PackageReference</c> element in the .csproj referenced by <see cref="ProjectFile"/>.
        /// </summary>
        internal IEnumerable<PackageReference> PackageReferences =>
            string.IsNullOrEmpty(ProjectFile)
            ? Enumerable.Empty<PackageReference>()
            : XDocument.Load(ProjectFile)
                .XPathSelectElements("//PackageReference")
                .Select(element => new PackageReference(
                    new PackageIdentity(
                        id: element.Attribute("Include")?.Value,
                        version: new NuGetVersion(element.Attribute("Version")?.Value)),
                    NuGetFramework.AnyFramework));

        internal string Sdk =>
            string.IsNullOrEmpty(ProjectFile)
            ? string.Empty
            : XDocument.Load(ProjectFile)
                .XPathSelectElements("//Project")
                .Select(element => element.Attribute("Sdk")?.Value)
                .FirstOrDefault()
            ?? string.Empty;

        internal bool UsesQuantumSdk =>
            Sdk.StartsWith("Microsoft.Quantum.Sdk");

        private const string AutoLoadPropertyName = "IQSharpLoadAutomatically";
        private bool? ShouldAutoLoad
        {
            get 
            {
                if (string.IsNullOrEmpty(ProjectFile)) return false;
                var elementValue = XDocument.Load(ProjectFile)
                    .XPathSelectElements($"//{AutoLoadPropertyName}")
                    .Select(element => element.Value)
                    .FirstOrDefault();
                if (string.IsNullOrWhiteSpace(elementValue)) return null;
                if (Boolean.TryParse(elementValue, out var shouldAutoLoad)) return shouldAutoLoad;
                return null;
            }
        }

        internal bool IncludeSubdirectories =>
            !string.IsNullOrEmpty(ProjectFile);

        /// <summary>
        /// Returns the list of .qs source file paths associated with the project.
        /// These are the files that should be compiled when building the project. 
        /// </summary>
        public IEnumerable<string> SourceFiles =>
            Directory.EnumerateFiles(RootFolder, "*.qs",
                IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        private AssemblyInfo? _assemblyInfo;

        internal AssemblyInfo? AssemblyInfo
        {
            get => File.Exists(_assemblyInfo?.Location) ? _assemblyInfo : null;
            set => _assemblyInfo = value;
        }

        internal string CacheDllPath =>
            Path.Combine(CacheFolder, CacheDllName);

        /// <summary>
        /// Returns the file name of the .dll to be built for this project.
        /// </summary>
        public string CacheDllName =>
            string.IsNullOrEmpty(ProjectFile)
                ? "__ws__.dll"
                : $"__ws__{string.Join("_", ProjectFile.Split(Path.GetInvalidFileNameChars()))}__.dll";

        /// <summary>
        ///     Creates a <see cref="Project"/> for the given workspace folder.
        ///     If there is exactly one .csproj in the given folder, and if we have not
        ///     been asked to skip auto-loading the project file, then this function
        ///     simply delegates to <see cref="FromProjectFile(string)"/> with the 
        ///     path of the .csproj file. Otherwise, a <see cref="Project"/> object
        ///     is returned without a reference to a .csproj file.
        /// </summary>
        /// <param name="rootFolder">The path to the workspace folder.</param>
        /// <param name="cacheFolder">The path to the assembly cache folder.</param>
        /// <param name="skipAutoLoadProject">Whether to skip auto-loading the project file.</param>
        /// <param name="logger">Used for reporting log messages.</param>
        /// <returns>The created <see cref="Project"/> object.</returns>
        internal static Project FromWorkspaceFolder(string rootFolder, string cacheFolder, bool skipAutoLoadProject, ILogger? logger = null)
        {
            var defaultProject = new Project(string.Empty, rootFolder, cacheFolder);
            if (skipAutoLoadProject)
            {
                logger?.LogDebug("Not looking for .csproj to load automatically, because skipAutoLoadProject was specified " +
                    "via --skipAutoLoadProject command line switch or via IQSHARP_SKIP_AUTO_LOAD_PROJECT environment variable.");
                return defaultProject;
            }

            var projectFiles = Directory.EnumerateFiles(rootFolder, "*.csproj", SearchOption.TopDirectoryOnly);
            if (!projectFiles.Any())
            {
                logger?.LogDebug($"No .csproj file found in workspace root {rootFolder}. Will compile only .qs files in this folder.");
                return defaultProject;
            }

            var projects = projectFiles
                .Select(projectFile => Project.FromProjectFile(projectFile, cacheFolder))
                .Where(project => project.UsesQuantumSdk);
            if (!projects.Any())
            {
                logger?.LogDebug($"No .csproj file referencing Microsoft.Quantum.Sdk found in workspace root {rootFolder}. " +
                    "Will compile only .qs files in this folder.");
                return defaultProject;
            }

            var defaultShouldAutoLoad = false; // REL0920: Change this default value in the future and update messaging below.
            var autoLoadProjects = projects.Where(project => project.ShouldAutoLoad.GetValueOrDefault(defaultShouldAutoLoad));
            if (autoLoadProjects.Count() != 1)
            {
                logger?.LogWarning($"Multiple .csproj files referencing Microsoft.Quantum.Sdk found in workspace root {rootFolder} " +
                    $"and are set to automatically load via the property {AutoLoadPropertyName}." +
                    "Project auto-load is currently supported for only a single project. " +
                    "Skipping project auto-load and will compile only .qs files in this folder.");
                return defaultProject;
            }

            var project = autoLoadProjects.Single();
            if (!project.ShouldAutoLoad.HasValue)
            {
                logger?.LogWarning($"Future deprecation warning: Found .csproj referencing Microsoft.Quantum.Sdk at {project.ProjectFile}, " +
                    $"but the property {AutoLoadPropertyName} was not set. A default value of false is assumed. " +
                    $"To load this project automatically, add <{AutoLoadPropertyName}>true</{AutoLoadPropertyName}> to the <PropertyGroup> " +
                    $"of {project.ProjectFile}. To suppress this warning without loading the project automatically, " +
                    $"add <{AutoLoadPropertyName}>false</{AutoLoadPropertyName}> to the <PropertyGroup> instead. " +
                    "This behavior may change in a future version of IQ#, and projects without the " +
                    $"{AutoLoadPropertyName} property may be loaded automatically by default.");
                return defaultProject;
            }

            if (!project.ShouldAutoLoad.GetValueOrDefault(defaultShouldAutoLoad))
            {
                logger?.LogDebug($"Found .csproj file {project.ProjectFile}, but {AutoLoadPropertyName} is disabled. " +
                    "Skipping load of this project. Will compile only .qs files in this folder.");
                return defaultProject;
            }

            logger?.LogInformation($"Loading .csproj file {project.ProjectFile}.");
            return project;
        }

        /// <summary>
        ///     Creates a <see cref="Project"/> referencing the given .csproj file.
        /// </summary>
        /// <param name="projectFile">The path to the .csproj file for the project.</param>
        /// <param name="cacheFolder">The path to the assembly cache folder.</param>
        /// <returns>The created <see cref="Project"/> object.</returns>
        internal static Project FromProjectFile(string projectFile, string cacheFolder) =>
            new Project(projectFile, Path.GetDirectoryName(projectFile), cacheFolder);

        private Project(string projectFile, string rootFolder, string cacheFolder)
        {
            ProjectFile = projectFile;
            RootFolder = rootFolder;
            CacheFolder = cacheFolder;
            Watchers = File.Exists(RootFolder)
                ? new FileSystemWatcher[] { CreateWatcher(RootFolder, "*.qs"), CreateWatcher(RootFolder, "*.csproj") }
                : Enumerable.Empty<FileSystemWatcher>();
        }

        private FileSystemWatcher CreateWatcher(string folder, string filter) =>
            new FileSystemWatcher(folder, filter)
            {
                IncludeSubdirectories = IncludeSubdirectories,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            };
    }
}
