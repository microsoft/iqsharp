// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.AzureClient;
using Microsoft.Quantum.IQSharp.Common;

using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    ///     A specialized resolver for MagicSymbols. 
    ///     It finds all types that inherit MagicSymbol on the current Assembly
    ///     and all the Assemblies in global references (including those
    ///     added via nuget Packages).
    /// </summary>
    public class MagicSymbolResolver : IMagicSymbolResolver
    {
        private AssemblyInfo[] kernelAssemblies;
        private Dictionary<AssemblyInfo, MagicSymbol[]> cache;
        private IServiceProvider services;
        private IReferences references;
        private IWorkspace workspace;
        private ILogger logger;

        /// <summary>
        ///     Constructs a new magic symbol resolver using the provided
        ///     services to search assembly references for subclasses of
        ///     <see cref="Microsoft.Jupyter.Core.MagicSymbol" />.
        /// </summary>
        public MagicSymbolResolver(IServiceProvider services, ILogger<MagicSymbolResolver> logger)
        {
            this.cache = new Dictionary<AssemblyInfo, MagicSymbol[]>();
            this.logger = logger;

            this.kernelAssemblies = new[]
            {
                new AssemblyInfo(typeof(MagicSymbolResolver).Assembly),
                new AssemblyInfo(typeof(AzureClient.AzureClient).Assembly)
            };
            this.services = services;
            this.references = services.GetService<IReferences>();
            this.workspace = services.GetService<IWorkspace>();
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
            if (symbolName == null || !symbolName.TrimStart().StartsWith("%")) return null;

            this.logger.LogDebug($"Looking for magic {symbolName}");

            foreach (var magic in FindAllMagicSymbols())
            {
                if (symbolName == magic.Name)
                {
                    this.logger.LogDebug($"Using magic {magic.Name}");
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

            lock (cache)
            {
                if (cache.TryGetValue(assm, out result))
                {
                    return result;
                }

                this.logger.LogInformation($"Looking for MagicSymbols in {assm.Assembly.FullName}");

                // If types from an assembly cannot be loaded, log a warning and continue.
                var allMagic = new List<MagicSymbol>();
                try
                {
                    var magicTypes = assm.Assembly
                        .GetTypes()
                        .Where(t =>
                        {
                            if (!t.IsClass && t.IsAbstract) { return false; }
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

                result = allMagic.ToArray();
                cache[assm] = result;
            }

            return result;
        }

        /// <inheritdoc />
        public IEnumerable<MagicSymbol> FindAllMagicSymbols() =>
            RelevantAssemblies().SelectMany(FindMagic);
    }
}
