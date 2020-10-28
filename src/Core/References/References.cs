// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Packaging.Core;

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
                IOptions<Settings> options
                )
        {
            Assemblies = QUANTUM_CORE_ASSEMBLIES.ToImmutableArray();
            Nugets = packages;
            Logger = logger;

            eventService?.TriggerServiceInitialized<IReferences>(this);

            var referencesOptions = options.Value;
            if (referencesOptions?.AutoLoadPackages is string autoLoadPkgs)
            {
                logger.LogInformation(
                    "Auto-load packages overridden by startup options: \"{0}\"",
                    referencesOptions.AutoLoadPackages
                );
                AutoLoadPackages = ParsePackages(autoLoadPkgs);
            }

            _metadata = new Lazy<CompilerMetadata>(() => new CompilerMetadata(this.Assemblies));

            AssemblyLoadContext.Default.Resolving += Resolve;
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
        private Lazy<CompilerMetadata> _metadata;

        private ILogger<References> Logger { get; }

        public event EventHandler<PackageLoadedEventArgs>? PackageLoaded;

        /// <summary>
        /// The plain list of Assemblies to be used as References. 
        /// </summary>
        public ImmutableArray<AssemblyInfo> Assemblies { get; private set; }

        public CompilerMetadata CompilerMetadata => _metadata.Value;

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
            PackageLoaded?.Invoke(this, new PackageLoadedEventArgs(pkg.Id, pkg.Version?.ToNormalizedString() ?? string.Empty, duration.Elapsed));
        }

        private void Reset()
        {
            _metadata = new Lazy<CompilerMetadata>(() => new CompilerMetadata(this.Assemblies));
        }

        /// <summary>
        /// Because the assemblies are loaded into memory, we need to provide this method to the AssemblyLoadContext
        /// such that the Workspace assembly or this assembly is correctly resolved when it is executed for simulation.
        /// </summary>
        public Assembly? Resolve(AssemblyLoadContext context, AssemblyName name) =>
            Assemblies.FirstOrDefault(a => a.Assembly.FullName == name.FullName)?.Assembly;
    }
}
