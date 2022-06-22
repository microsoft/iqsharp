﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Quantum.IQSharp
{

    /// <summary>
    /// Default implementation of IReferences.
    /// This service keeps track of references (assemblies) needed for compilation
    /// and exposes them in the different formats, depending on what each
    /// compiler needs.
    /// </summary>
    public class References : IReferences
    {
        /// <summary>
        ///     Settings that control how references are discovered and loaded.
        /// </summary>
        public class Settings
        {
            /// <summary>
            ///      A list of packages to be loaded when the kernel initially
            ///      starts. Package names should be separated by <c>,</c>, and
            ///      may have optional version specifiers. To suppress all
            ///      automatic package loading, the string <c>"$null"</c> can
            ///      be provided.
            /// </summary>
            public string? AutoLoadPackages { get; set; }
        }

        /// <summary>
        /// The list of assemblies that are automatically included for compilation. Namely:
        ///   * Quantum.Core
        ///   * Quantum.Intrinsic
        /// </summary>
        public static readonly AssemblyInfo[] QUANTUM_CORE_ASSEMBLIES =
        {
            new AssemblyInfo(typeof(Simulation.Core.Operation<,>).Assembly),
            new AssemblyInfo(typeof(Intrinsic.X).Assembly),
            new AssemblyInfo(typeof(Core.EntryPoint).Assembly)
        };


        /// <summary>
        /// The list of Packages that are automatically included for compilation. Namely:
        ///   * Microsoft.Quantum.Standard
        ///   * Microsoft.Quantum.Standard.Visualization
        /// </summary>
        public readonly ImmutableList<string> AutoLoadPackages =
            ImmutableList.Create(
                "Microsoft.Quantum.Standard",
                "Microsoft.Quantum.Standard.Visualization"
            );

        private ImmutableList<string> ParsePackages(string pkgs) =>
            pkgs.Trim() == "$null"
                ? ImmutableList<string>.Empty
                : pkgs
                    .Split(",")
                    .Select(pkg => pkg.Trim())
                    .ToImmutableList();

        /// <summary>
        /// Create a new References list populated with the list of DEFAULT_ASSEMBLIES 
        /// </summary>
        public References(
            INugetPackages packages,
            IEventService eventService,
            ILogger<References> logger,
            IOptions<Settings> options,
            IPerformanceMonitor performanceMonitor
        )
        {
            this.performanceMonitor = performanceMonitor;
            Assemblies = QUANTUM_CORE_ASSEMBLIES.ToImmutableArray();
            Nugets = packages;
            Logger = logger;

            var referencesOptions = options.Value;
            if (referencesOptions?.AutoLoadPackages is string autoLoadPkgs)
            {
                logger.LogInformation(
                    "Auto-load packages overridden by startup options: \"{0}\"",
                    referencesOptions.AutoLoadPackages
                );
                AutoLoadPackages = ParsePackages(autoLoadPkgs);
            }

            // The call to Reset below ensures that _metadata is not null.
            Reset();
            Debug.Assert(_metadata != null, "Reset did not initialize compiler metadata.");

            AssemblyLoadContext.Default.Resolving += Resolve;

            eventService?.TriggerServiceInitialized<IReferences>(this);
        }

        /// <inheritdoc/>
        public void LoadDefaultPackages()
        {
            foreach (var pkg in AutoLoadPackages)
            {
                try
                {
                    AddPackage(pkg).Wait();
                }
                catch (AggregateException e)
                {
                    Logger?.LogError($"Unable to load package '{pkg}':  {e.InnerException.Message}");
                }
            }
        }

        /// Manages nuget packages.
        internal INugetPackages Nugets { get; }
        private readonly IPerformanceMonitor performanceMonitor;
        private Task<CompilerMetadata> _metadata;
        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        private ILogger<References> Logger { get; }

        public event EventHandler<PackageLoadedEventArgs>? PackageLoaded;

        /// <summary>
        /// The plain list of Assemblies to be used as References. 
        /// </summary>
        public ImmutableArray<AssemblyInfo> Assemblies { get; private set; }

        public CompilerMetadata CompilerMetadata => _metadata.Result;

        /// <summary>
        /// The list of Nuget Packages that are available for compilation and execution.
        /// </summary>
        public virtual IEnumerable<string>? Packages =>
            Nugets
                ?.Items
                ?.Select(p => $"{p.Id}::{p.Version}");

        /// <summary>
        /// Adds the given assemblies.
        /// </summary>
        public void AddAssemblies(params AssemblyInfo[] assemblies)
        {
            lock (this)
            {
                Assemblies = Assemblies.Union(assemblies).ToImmutableArray();
                Reset();
            }
        }

        /// <summary>
        /// Adds the libraries from the given nuget package to the list of assemblies.
        /// If version is not provided. It automatically picks up the latest version.
        /// </summary>
        public async Task AddPackage(string name, Action<string>? statusCallback = null)
        {
            if (Nugets == null)
            {
                throw new InvalidOperationException("Packages can be only added to the global references collection");
            }

            var duration = Stopwatch.StartNew();

            var pkg = await Nugets.Add(name, statusCallback);
            AddAssemblies(Nugets.Assemblies.ToArray());

            duration.Stop();
            Logger?.LogInformation("Loaded package {Id}::{Version} in {Time}.", pkg.Id, pkg.Version?.ToNormalizedString() ?? string.Empty, duration.Elapsed);
            PackageLoaded?.Invoke(this, new PackageLoadedEventArgs(pkg.Id, pkg.Version?.ToNormalizedString() ?? string.Empty, duration.Elapsed));
        }

        private void Reset()
        {
            var oldMetadata = _metadata;
            // Begin loading metadata in the background.
            _metadata = Task.Run(
                async () =>
                {
                    // Don't run multiple assembly reference loads at a time.
                    if (oldMetadata != null)
                    {
                        await oldMetadata;
                    }
                    using var perfTask = performanceMonitor.BeginTask("Resetting reference metadata.", "reset-refs-meta");
                    var result = new CompilerMetadata(this.Assemblies.Where(IsAssemblyPossiblyQSharpReference));
                    return result;
                },
                tokenSource.Token
            );
        }

        private static bool IsAssemblyPossiblyQSharpReference(AssemblyInfo arg) =>
            !Regex.Match(
                arg.Assembly.GetName().Name,
                // Reference filtering should match the filtering at
                // https://github.com/microsoft/qsharp-compiler/blob/c3d1a09f70960d09af68e805294962e7e6c690d8/src/QuantumSdk/Sdk/Sdk.targets#L70.
                "(?i)system.|mscorlib|netstandard.library|microsoft.netcore.app|csharp|fsharp|microsoft.visualstudio|microsoft.testplatform|microsoft.codeanalysis|fparsec|newtonsoft|roslynwrapper|yamldotnet|markdig|serilog"
            ).Success;

        /// <summary>
        /// Because the assemblies are loaded into memory, we need to provide this method to the AssemblyLoadContext
        /// such that the Workspace assembly or this assembly is correctly resolved when it is executed for simulation.
        /// </summary>
        public Assembly? Resolve(AssemblyLoadContext context, AssemblyName name) 
        {
            bool Compare(AssemblyInfo a) =>
                // If the Assembly requested doesn't include version, then check only for the simple name
                // of the assembly, otherwise check for the full name (including PublicKey)
                (name.Version == null)
                    ? a.Assembly.GetName().Name == name.Name
                    : a.Assembly.FullName == name.FullName;
            
            return Assemblies.FirstOrDefault(Compare)?.Assembly;
        }
    }
}
