// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.IO;
using System.Reflection;
using System.Xml;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.Logging;
using Eval = Microsoft.Build.Evaluation;

namespace Microsoft.Quantum.IQSharp;

/// <summary>
/// Default implementation of ICompilerService.
/// This service is capable of building .net core assemblies on the fly from Q# code.
/// </summary>
public partial class CompilerService
{

    private class MSBuildLogger : Microsoft.Build.Utilities.Logger
    {
        private CompilerService service;
        public MSBuildLogger(CompilerService service)
        {
            this.service = service;
        }
        public override void Initialize(IEventSource eventSource)
        {
            eventSource.MessageRaised += (sender, e) =>
            {
                switch (e.Importance)
                {
                    case MessageImportance.High:
                        service.Logger.LogTrace("MSBuild message: {Code} {Message}", e.Code, e.Message);
                        break;
                    case MessageImportance.Normal:
                        service.Logger.LogTrace("MSBuild message: {Code} {Message}", e.Code, e.Message);
                        break;
                }
            };
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
        new string[] { "QSharpLangVersion" };


    private ProjectInstance? QsProjectInstance(string projectFile, out Dictionary<string, string?> metadata)
    {
        metadata = new Dictionary<string, string?>();
        if (!File.Exists(projectFile))
        {
            return null;
        }

        var loggers = new Build.Framework.ILogger[] { new MSBuildLogger(this) };
        var properties = new Dictionary<string, string>();

        // restore project (requires reloading the project after for the restore to take effect)
        var succeed = LoadAndApply(projectFile, properties, project =>
            project.CreateProjectInstance().Build("Restore", loggers));
        if (!succeed)
        {
            this.Logger.LogError($"Failed to restore project '{projectFile}'.");
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
                this.Logger.LogError($"Failed to resolve assembly references for project '{projectFile}'.");
            }

            return instance.Targets.ContainsKey("QSharpCompile") ? instance : null;
        });
    }

    internal IEnumerable<string> TargetPackageAssemblyPaths(string? targetId, string? targetCapability = null)
    {
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
        // var nodeReader = new XmlNodeReader(xmlDoc);
        using (var file = File.OpenWrite(projectPath))
        {
            var xmlWriter = XmlWriter.Create(new StreamWriter(file));
            xmlDoc.WriteTo(xmlWriter);
            xmlWriter.Flush();
        }
        // var projectCollection = new Eval.ProjectCollection();
        // var project = projectCollection.LoadProject(projectPath);
        // var instance = project.CreateProjectInstance();
        // var binLogger = new Microsoft.Build.Logging.BinaryLogger();
        // binLogger.Parameters = "iqsharp.binlog";
        // var loggers = new Microsoft.Build.Framework.ILogger[] { new MSBuildLogger(this), binLogger };
        // if (!instance.Build(target: "Restore", loggers))
        // {
        //     Logger.LogError("MSBuild failed to run Restore target on temporary project.");
        // };

        // instance = project.CreateProjectInstance();
        // if (!instance.Build(target: "ResolveTargetPackage", loggers))
        // {
        //     Logger.LogError("MSBuild failed to run ResolveTargetPackage target on temporary project.");
        // }

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
