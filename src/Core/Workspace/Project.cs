// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Microsoft.Quantum.IQSharp
{
    internal static class ProjectExtensions
    {
        public static string ToLogString(this IEnumerable<Project> projects) =>
            string.Join(";", projects.Select(p => p.ProjectFile));
    }

    internal class ProjectFileComparer : EqualityComparer<Project>
    {
        private readonly bool IgnoreCase =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public override bool Equals(Project p1, Project p2) =>
            string.Equals(p1.ProjectFile, p2.ProjectFile, IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        public override int GetHashCode(Project project) =>
            IgnoreCase ? project.ProjectFile.ToLowerInvariant().GetHashCode() : project.ProjectFile.GetHashCode();
    }

    public class Project
    {
        public readonly string ProjectFile;
        internal readonly string RootFolder;
        internal readonly string CacheFolder;
        internal readonly IEnumerable<FileSystemWatcher> Watchers;

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

        internal bool IncludeSubdirectories =>
            !string.IsNullOrEmpty(ProjectFile);

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
        /// <returns>The created <see cref="Project"/> object.</returns>
        internal static Project FromWorkspaceFolder(string rootFolder, string cacheFolder, bool skipAutoLoadProject)
        {
            var projectFiles = Directory.EnumerateFiles(rootFolder, "*.csproj", SearchOption.TopDirectoryOnly);
            return (skipAutoLoadProject || projectFiles.Count() != 1)
                ? new Project(string.Empty, rootFolder, cacheFolder)
                : Project.FromProjectFile(projectFiles.Single(), cacheFolder);
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
