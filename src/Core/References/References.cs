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
    internal class ReferencesOptions
    {
        public string? BuiltInPackages { get; set; }
    }

    /// <summary>
    /// Default implementation of IReferences.
    /// This service keeps track of references (assemblies) needed for compilation
    /// and exposes them in the different formats, depending on what each
    /// compiler needs.
    /// </summary>
    public class References : IReferences
    {
        /// <summary>
        /// The list of assemblies that are automatically included for compilation. Namely:
        ///   * Quantum.Core
        ///   * Quantum.Intrinsic
        /// </summary>
        public static readonly AssemblyInfo[] QUANTUM_CORE_ASSEMBLIES =
        {
            new AssemblyInfo(typeof(Simulation.Core.Operation<,>).Assembly),
            new AssemblyInfo(typeof(Intrinsic.X).Assembly)
        };


        /// <summary>
        /// The list of Packages that are automatically included for compilation. Namely:
        ///   * Microsoft.Quantum.Standard
        /// </summary>
        public readonly ImmutableList<string> BUILT_IN_PACKAGES =
            ImmutableList.Create(
                "Microsoft.Quantum.Standard"
            );

        /// <summary>
        /// Create a new References list populated with the list of DEFAULT_ASSEMBLIES 
        /// </summary>
        public References(
                INugetPackages packages,
                IEventService eventService,
                ILogger<References> logger,
                IConfiguration configuration
                )
        {
            Assemblies = QUANTUM_CORE_ASSEMBLIES.ToImmutableArray();
            Nugets = packages;

            eventService?.TriggerServiceInitialized<IReferences>(this);

            var referencesOptions = configuration.Get<ReferencesOptions>();
            if (!(referencesOptions?.BuiltInPackages is null))
            {
                BUILT_IN_PACKAGES = referencesOptions
                    .BuiltInPackages
                    .Split(",")
                    .Select(pkg => pkg.Trim())
                    .ToImmutableList();
            }

            foreach (var pkg in BUILT_IN_PACKAGES)
            {
                try
                {
                    this.AddPackage(pkg).Wait();
                }
                catch (AggregateException e)
                {
                    logger.LogError($"Unable to load package '{pkg}':  {e.InnerException.Message}");
                }
            }

            _metadata = new Lazy<CompilerMetadata>(() => new CompilerMetadata(this.Assemblies));

            AssemblyLoadContext.Default.Resolving += Resolve;
        }

        /// Manages nuget packages.
        internal INugetPackages Nugets { get; }
        private Lazy<CompilerMetadata> _metadata;

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
            Assemblies = Assemblies.Union(assemblies).ToImmutableArray();
            Reset();
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
            PackageLoaded?.Invoke(this, new PackageLoadedEventArgs(pkg.Id, pkg.Version.ToNormalizedString(), duration.Elapsed));
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
