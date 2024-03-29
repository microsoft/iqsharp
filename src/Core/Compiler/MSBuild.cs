// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// This file contains code ported from the QsCompiler project and used to
// build temporary projects in order to access MSBuild properties and targets
// generated by Microsoft.Quantum.Sdk.

#nullable enable

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Linq;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.QsCompiler;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using Eval = Microsoft.Build.Evaluation;

namespace Microsoft.Quantum.IQSharp;

public partial class CompilerService
{
    public record MSBuildMetadata(
        Version Version,
        string RootPath,
        string Name,
        string Path
    );

    /// <summary>
    ///      Used to map <see cref="Microsoft.Build.Framework.ILogger" /> calls
    ///      to <see cref="Microsoft.Extensions.Logging.ILogger" /> instances.
    /// </summary>
    private class MSBuildLogger : Microsoft.Build.Utilities.Logger, IDisposable
    {
        private Microsoft.Extensions.Logging.ILogger Logger;
        private readonly Dictionary<string, IDisposable> ProjectScopes = new();

        // Define mappings between MSBuild and ILogger log levels. By default,
        // we will downgrade severity of MSBuild errors, since we may be able
        // to recover using heuristics.
        public LogLevel ErrorLevel { get; init; } = LogLevel.Warning;
        public LogLevel WarningLevel { get; init; } = LogLevel.Debug;
        public LogLevel TraceLevel { get; init; } = LogLevel.Trace;

        // We ask for a logger scoped to this class to make sure that MSBuild
        // logs are easy to filter and select on.
        public MSBuildLogger(Microsoft.Extensions.Logging.ILogger<MSBuildLogger> logger)
        {
            this.Logger = logger;
        }

        public override void Initialize(IEventSource eventSource)
        {
            eventSource.MessageRaised += (sender, e) =>
                Logger.Log(TraceLevel, "MSBuild message: {Code} {Message}", e.Code, e.Message);

            eventSource.WarningRaised += (sender, e) =>
                Logger.Log(WarningLevel, "MSBuild warning: {Code} {Message}", e.Code, e.Message);

            eventSource.ErrorRaised += (sender, e) =>
                Logger.Log(ErrorLevel, "MSBuild error: {Code} {Message}", e.Code, e.Message);

            eventSource.ProjectStarted += (sender, e) =>
            {
                var scope = Logger.BeginScope("Starting to build project {Project}.", e.ProjectFile);
                ProjectScopes[e.ProjectFile] = scope;
            };

            eventSource.ProjectFinished += (sender, e) =>
            {
                if (ProjectScopes.TryGetValue(e.ProjectFile, out var scope))
                {
                    Logger.LogTrace("Done building project {Project}.", e.ProjectFile);
                    ProjectScopes.Remove(e.ProjectFile);
                    scope.Dispose();
                }
            };
        }

        public void Dispose()
        {
            foreach (var scope in ProjectScopes.Values)
            {
                scope.Dispose();
            }
            ProjectScopes.Clear();
        }
    }

    private static T LoadAndApply<T>(string projectFile, IDictionary<string, string> properties, Func<Eval.Project, T> query)
    {
        if (!File.Exists(projectFile))
        {
            throw new ArgumentException("given project file is null or does not exist", nameof(projectFile));
        }

        Eval.Project? project = null;
        try
        {
            // Unloading the project unloads the project but *doesn't* clear the cache to be resilient to inconsistent states.
            // Hence we actually need to unload all projects, which does make sure the cache is cleared and changes on disk are reflected.
            // See e.g. https://github.com/Microsoft/msbuild/issues/795
            Eval.ProjectCollection.GlobalProjectCollection.UnloadAllProjects(); // needed due to the caching behavior of MS build
            project = new Eval.Project(projectFile, properties, ToolLocationHelper.CurrentToolsVersion);
            return query(project);
        }
        finally
        {
            if (project != null)
            {
                Eval.ProjectCollection.GlobalProjectCollection?.UnloadProject(project);
            }
        }
    }

    private readonly ImmutableArray<string> supportedQsFrameworks =
        ImmutableArray.Create("netstandard2.", "netcoreapp2.", "netcoreapp3.", "net6.");

    private bool IsSupportedQsFramework(string framework) =>
        framework != null
        ? this.supportedQsFrameworks.Any(framework.ToLowerInvariant().StartsWith)
        : false;

    private static bool GeneratePackageInfo(string packageName) =>
        packageName.StartsWith("microsoft.quantum.", StringComparison.InvariantCultureIgnoreCase);

    private static readonly IEnumerable<string> PropertiesToTrack =
        new[] { "QSharpLangVersion" };


    private ProjectInstance? QsProjectInstance(string projectFile, out Dictionary<string, string?> metadata)
    {
        metadata = new Dictionary<string, string?>();
        if (!File.Exists(projectFile))
        {
            return null;
        }

        using var buildLogger = ActivatorUtilities.CreateInstance<MSBuildLogger>(this.services);
        var loggers = new List<Microsoft.Build.Framework.ILogger>
        {
            buildLogger
        };

        if (!string.IsNullOrEmpty(settings.MSBuildBinlogPath))
        {
            var binLogger = new Microsoft.Build.Logging.BinaryLogger();
            var binLogPath = Path.GetFullPath(settings.MSBuildBinlogPath);
            binLogger.Parameters = binLogPath;
            Logger?.LogDebug("Writing MSBuild binlog to {BinLogPath}.", binLogPath);
            loggers.Add(binLogger);
        }

        var properties = new Dictionary<string, string>();

        // restore project (requires reloading the project after for the restore to take effect)
        var succeed = LoadAndApply(projectFile, properties, project =>
            project.CreateProjectInstance().Build("Restore", loggers));
        if (!succeed)
        {
            this.Logger.LogError("Failed to restore project '{ProjectFile}'.", projectFile);
        }

        // build the project instance and returns it if it is indeed a Q# project
        return LoadAndApply(projectFile, properties, project =>
        {
            var instance = project.CreateProjectInstance();
            var target = instance.Targets.ContainsKey("ResolveTargetPackage")
                ? "ResolveTargetPackage"
                : "ResolveAssemblyReferencesDesignTime";
            succeed = instance.Build(target, loggers);
            if (!succeed)
            {
                this.Logger.LogError("Failed to resolve assembly references for project '{ProjectFile}'.", projectFile);
            }

            return instance.Targets.ContainsKey("QSharpCompile") ? instance : null;
        });
    }

    internal IEnumerable<string> TargetPackageAssemblyPaths(string? targetId, string? targetCapability = null)
    {
        // See if MSBuild was registered at startup and give some logging information either way.
        if (services.GetService<MSBuildMetadata>() is {} msBuild)
        {
            Logger.LogInformation("Found MSBuild instance: {MSBuildInstance}", msBuild);
            if (settings.ForceTargetingHeuristics)
            {
                Logger.LogInformation("Configured to bypass MSBuild and use heuristics to compute targeting packages.");
                return TargetPackageAssemblyPathsFromHeuristics(targetId, targetCapability).Collect().Result;
            }
            else
            {
                return TargetPackageAssemblyPathsFromMSBuild(targetId, targetCapability);
            }
        }
        else
        {
            Logger.LogWarning("No MSBuild instance was found during kernel startup, trying to use best-known values for self-contained development.");
            return TargetPackageAssemblyPathsFromHeuristics(targetId, targetCapability).Collect().Result;

        }
    }

    internal string? GuessProviderPackageForTarget(string? targetId, string? targetCapability = null)
    {
        var capability = TargetCapabilityModule.FromName(targetCapability).AsObj();
        if (targetId is {} target)
        {
            bool Matches(string pattern) =>
                target?.Contains(pattern, StringComparison.InvariantCultureIgnoreCase) ?? false;

            if (Matches("quantinuum") && capability is { ClassicalCompute: var classical } && classical != ClassicalComputeModule.Full)
            {
                return "Microsoft.Quantum.Type1.Core";
            }
            else if (Matches("qci"))
            {
                return "Microsoft.Quantum.Type3.Core";
            }
            else if (Matches("rigetti"))
            {
                return "Microsoft.Quantum.Type4.Core";
            }
        }

        return null;
    }


    internal async IAsyncEnumerable<string> TargetPackageAssemblyPathsFromHeuristics(string? targetId, string? targetCapability = null)
    {
        var targetPackage = GuessProviderPackageForTarget(targetId, targetCapability);
        if (targetPackage is null)
        {
            yield break;
        }
        var version = ((AssemblyInformationalVersionAttribute)(typeof(QsCompiler.AssemblyLoader)
            .Assembly
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
            .Single()))
            .InformationalVersion;
        var targetPackageIdentity = new PackageIdentity(targetPackage, new NuGet.Versioning.NuGetVersion(version));

        var packages = services.GetRequiredService<INugetPackages>();
        await packages.Get(
            targetPackageIdentity,
            (msg) => Logger.LogDebug("NuGet message while using target pkg heuristics: {Message}", msg)
        );

        var localPackagesFinder =
            packages.GlobalPackagesSource.GetResource<FindLocalPackagesResource>();
        var downloaded = localPackagesFinder.GetPackage(targetPackageIdentity, new NuGetLogger(Logger), default);
        if (downloaded is null)
        {
            Logger?.LogDebug("Local package {Package} not found while searching for target package assemblies, skipping.", targetPackageIdentity);
            yield break;
        }
        var packageReader = downloaded.GetReader();

        // Look for props files that could tell us what target packages we need.
        var xmlNsManager = new XmlNamespaceManager(new NameTable());
        xmlNsManager.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003");
        var buildItems = packageReader.GetBuildItems();
        foreach (var buildItem in buildItems.SelectMany(items => items.Items))
        {
            var path = Path.Join(Path.GetDirectoryName(downloaded.Path), buildItem);
            using var stream = File.OpenText(path);
            var xmlDoc = XDocument.Load(stream);
            var expectedName = "QscRef_" + targetPackage.Replace(".", "_");
            var node = xmlDoc.XPathSelectElements($"//msb:{expectedName}", xmlNsManager)
                .Select(e => e.Value)
                .Single()
                .Replace("$(MSBuildThisFileDirectory)", Path.GetDirectoryName(path));
            yield return node;
        }
    }

    /// <summary>
    ///      Enumerates over all assembly files contained in target packages
    ///      for the given execution target and target capabilities.
    /// </summary>
    /// <remarks>
    ///      This method uses MSBuild to compile a temporary project, and as
    ///      a result, depends on being able to write temporary files to disk
    ///      and on being able to call into Microsoft.Build assemblies.
    /// </remarks>
    internal IEnumerable<string> TargetPackageAssemblyPathsFromMSBuild(string? targetId, string? targetCapability = null)
    {
        // See if MSBuild was registered at startup and give some logging information either way.
        Debug.Assert(services.GetService<MSBuildMetadata>() is {} msBuild);

        var xmlDoc = new XmlDocument();
        var root = xmlDoc.CreateElement("Project");
        var version = ((AssemblyInformationalVersionAttribute)(typeof(QsCompiler.AssemblyLoader)
            .Assembly
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
            .Single()))
            .InformationalVersion;
        root.SetAttribute("Sdk", $"Microsoft.Quantum.Sdk/{version}");
        xmlDoc.AppendChild(root);

        var propertyGroup = xmlDoc.CreateElement("PropertyGroup");
        root.AppendChild(propertyGroup);

        void AddProperty(string name, string? value)
        {
            var element = xmlDoc.CreateElement(name);
            element.InnerText = value ?? "";
            propertyGroup.AppendChild(element);
        }

        AddProperty("TargetFramework", "net6.0");
        AddProperty("ExecutionTarget", targetId);
        AddProperty("TargetCapability", targetCapability);
        AddProperty("OutputType", "Exe");

        var projectPath = Path.ChangeExtension(Path.GetTempFileName(), "csproj");
        using (var file = File.OpenWrite(projectPath))
        {
            var xmlWriter = XmlWriter.Create(new StreamWriter(file));
            xmlDoc.WriteTo(xmlWriter);
            xmlWriter.Flush();
        }

        var instance = QsProjectInstance(projectPath, out var metadata);
        System.Diagnostics.Debug.Assert(instance != null);
        var evaluatedTargetAssemblies = instance
            .Items
            .Where(item => item.ItemType == "ResolvedTargetSpecificDecompositions")
            .Select(item => Path.GetFullPath(item.EvaluatedInclude))
            .ToList();
        Logger.LogDebug("Evaluated temporary project for package assembly paths and got {NItems} items.", evaluatedTargetAssemblies.Count);
        return evaluatedTargetAssemblies;
    }
}
