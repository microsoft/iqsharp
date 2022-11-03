// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;

using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    ///     A specialized resolver for MagicSymbols. 
    ///     It finds all types that inherit MagicSymbol on the current Assembly
    ///     and all the Assemblies in global references (including those
    ///     added via nuget Packages).
    /// </summary>
    public class MagicSymbolResolver : IMagicSymbolResolver
    {
        
        /// <summary>
        ///     The simple names of those assemblies which do not need to be
        ///     searched for display encoders or magic commands.
        /// </summary>
        public static readonly IImmutableSet<string> MundaneAssemblies =
            new string[]
            {
                // These assemblies are classical libraries used by IQ#, and
                // do not contain any quantum code.
                "NumSharp.Core",
                "Newtonsoft.Json",
                "Microsoft.CodeAnalysis.CSharp.resources",

                // These assemblies are part of the Quantum Development Kit
                // built before IQ# (in dependency ordering), and thus cannot
                // contain types relevant to IQ#. While it doesn't hurt to scan
                // these assemblies, we can leave them out for performance.
                "Microsoft.Quantum.QSharp.Core",
                "Microsoft.Quantum.QSharp.Foundation",
                "Microsoft.Quantum.Simulation.QCTraceSimulatorRuntime",
                "Microsoft.Quantum.Targets.Interfaces",
                "Microsoft.Quantum.Simulators",
                "Microsoft.Quantum.Simulation.Common",
                "Microsoft.Quantum.Runtime.Core",
            }
            .ToImmutableHashSet();

        private readonly List<AssemblyInfo> kernelAssemblies = new List<AssemblyInfo>();
        private readonly Dictionary<string, MagicSymbol[]> assemblySymbolCache;
        private readonly Dictionary<string, MagicSymbol> resolutionCache;
        private IServiceProvider services;
        private IReferences references;
        private IWorkspace workspace;
        private ILogger logger;

        /// <summary>
        ///     Constructs a new magic symbol resolver using the provided
        ///     services to search assembly references for subclasses of
        ///     <see cref="Microsoft.Jupyter.Core.MagicSymbol" />.
        /// </summary>
        public MagicSymbolResolver(IServiceProvider services, ILogger<MagicSymbolResolver> logger, IEventService eventService)
        {
            this.assemblySymbolCache = new Dictionary<string, MagicSymbol[]>();
            this.resolutionCache = new Dictionary<string, MagicSymbol>();
            this.logger = logger;

            this.services = services;
            this.references = services.GetService<IReferences>();
            this.workspace = services.GetService<IWorkspace>();

            // Add the assembly containing this type to the resolver.
            this.AddKernelAssembly<MagicSymbolResolver>();

            eventService?.TriggerServiceInitialized<IMagicSymbolResolver>(this);
        }


        /// <summary>
        ///     Enumerates over all assemblies that should be searched
        ///     when resolving symbols.
        /// </summary>
        private IEnumerable<AssemblyInfo> RelevantAssemblies()
        {
            workspace.Initialization.Wait();

            foreach (var asm in this.kernelAssemblies)
            {
                yield return asm;
            }

            foreach (var asm in references.Assemblies)
            {
                yield return asm;
            }
        }

        /// <summary>
        ///     Resolves a given symbol name into a Q# symbol
        ///     by searching through all relevant assemblies.
        /// </summary>
        /// <returns>
        ///     The symbol instance if resolution is successful, otherwise <c>null</c>.
        /// </returns>
        /// <remarks>
        ///     If the symbol to be resolved contains a dot,
        ///     it is treated as a fully qualified name, and will
        ///     only be resolved to a symbol whose name matches exactly.
        ///     Symbol names without a dot are resolved to the first symbol
        ///     whose base name matches the given name.
        /// </remarks>
        public MagicSymbol? Resolve(string symbolName)
        {
            if (symbolName == null || !symbolName.TrimStart().StartsWith("%"))
            {
                return null;
            }
            lock (resolutionCache)
            {
                if (resolutionCache.TryGetValue(symbolName, out var cachedSymbol))
                {
                    return cachedSymbol;
                }
            }

            this.logger.LogDebug($"Looking for magic {symbolName}");

            foreach (var magic in FindAllMagicSymbols())
            {
                if (symbolName == magic.Name)
                {
                    this.logger.LogDebug($"Using magic {magic.Name}");
                    lock (resolutionCache)
                    {
                        resolutionCache[symbolName] = magic;
                    }
                    return magic;
                }
            }

            return null;
        }

        IEnumerable<ISymbol> ISymbolResolver.MaybeResolvePrefix(string symbolPrefix) =>
            FindAllMagicSymbols()
            .Where(symbol => symbol.Name.StartsWith(symbolPrefix))
            .OrderBy(symbol => symbol.Name);

        /// <inheritdoc />
        public IEnumerable<MagicSymbol> FindMagic(AssemblyInfo assm)
        {
            var result = new MagicSymbol[0];
            // If the assembly cannot possibly contain magic symbols, skip it
            // here.
            var name = assm.Assembly.GetName().Name;
            if (name.StartsWith("System.") || MundaneAssemblies.Contains(name))
            {
                return result;
            }

            lock (assemblySymbolCache)
            {
                if (assemblySymbolCache.TryGetValue(assm.Assembly.FullName, out result))
                {
                    return result;
                }

                this.logger.LogInformation($"Looking for MagicSymbols in {assm.Assembly.FullName}");
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                // If types from an assembly cannot be loaded, log a warning and continue.
                var allMagic = new List<MagicSymbol>();
                try
                {
                    var magicTypes = assm.Assembly
                        .GetTypes()
                        .Where(t =>
                        {
                            if (!t.IsClass || t.IsAbstract) { return false; }
                            var matched = t.IsSubclassOf(typeof(MagicSymbol));

                            // This logging statement is expensive, so we only run it when we need to for debugging.
                            #if DEBUG
                            this.logger.LogDebug("Class {Class} subclass of MagicSymbol? {Matched}", t.FullName, matched);
                            #endif

                            return matched;
                        });

                    foreach (var t in magicTypes)
                    {
                        try
                        {
                            var symbol = ActivatorUtilities.CreateInstance(services, t);
                            if (symbol is MagicSymbol magic)
                            {
                                allMagic.Add(magic);
                                this.logger.LogInformation($"Found MagicSymbols {magic.Name} ({t.FullName})");
                            }
                            else if (symbol is {})
                            {
                                throw new Exception($"Unable to create magic symbol of type {t}; service activator returned an object of type {symbol.GetType()}, which is not a subtype of MagicSymbol.");
                            }
                            else if (symbol == null)
                            {
                                throw new Exception($"Unable to create magic symbol of type {t}; service activator returned null.");
                            }
                        }
                        catch (Exception e)
                        {
                            this.logger.LogWarning(e, $"Unable to create instance of MagicSymbol {t.FullName}; service activator threw an exception of type {e.GetType()}. Magic will not be enabled.\nMessage: {e.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(
                        ex,
                        "Encountered exception loading types from {AssemblyName}.",
                        assm.Assembly.FullName
                    );
                }

                logger.LogInformation("Took {Elapsed} to scan {Assembly} for magic symbols.", stopwatch.Elapsed, assm.Assembly.FullName);
                result = allMagic.ToArray();
                assemblySymbolCache[assm.Assembly.FullName] = result;
            }

            return result;
        }

        /// <inheritdoc />
        public IEnumerable<MagicSymbol> FindAllMagicSymbols() =>
            RelevantAssemblies()
            .AsParallel()
            .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
            .SelectMany(FindMagic);

        /// <inheritdoc />
        public IMagicSymbolResolver AddKernelAssembly<TAssembly>()
        {
            this.kernelAssemblies.Add(new AssemblyInfo(typeof(TAssembly).Assembly));
            return this;
        }
    }
}
