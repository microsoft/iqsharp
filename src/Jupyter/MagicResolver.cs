// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;

using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    ///     A specialized resolver for MagicSymbols. 
    ///     It finds all types that inherit MagicSymbol on the current Assembly
    ///     and all the Assemblies in global references (including those
    ///     added via nuget Packages).
    /// </summary>
    public class MagicSymbolResolver : ISymbolResolver
    {
        private AssemblyInfo kernelAssembly;
        private Dictionary<AssemblyInfo, MagicSymbol[]> cache;
        private IServiceProvider services;
        private IReferences references;
        private ILogger logger;

        /// <summary>
        ///     Constructs a new magic symbol resolver using the provided
        ///     services to search assembly references for subclasses of
        ///     <see cref="Microsoft.Jupyter.Core.MagicSymbol" />.
        /// </summary>
        public MagicSymbolResolver(IServiceProvider services, ILogger<IQSharpEngine> logger)
        {
            this.cache = new Dictionary<AssemblyInfo, MagicSymbol[]>();
            this.logger = logger;

            this.kernelAssembly = new AssemblyInfo(typeof(IQSharpEngine).Assembly);
            this.services = services;
            this.references = services.GetService<IReferences>();
        }


        /// <summary>
        ///     Enumerates over all assemblies that should be searched
        ///     when resolving symbols.
        /// </summary>
        private IEnumerable<AssemblyInfo> RelevantAssemblies()
        {
            yield return this.kernelAssembly;

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
        public ISymbol Resolve(string symbolName)
        {
            if (symbolName == null || !symbolName.TrimStart().StartsWith("%")) return null;

            this.logger.LogDebug($"Looking for magic {symbolName}");

            foreach (var magic in RelevantAssemblies().SelectMany(FindMagic))
            {
                if (symbolName.StartsWith(magic.Name))
                {
                    this.logger.LogDebug($"Using magic {magic.Name}");
                    return magic;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the MagicSymbols inside an assembly, and returns an instance of each.
        /// </summary>
        public IEnumerable<MagicSymbol> FindMagic(AssemblyInfo assm)
        {
            var result = new MagicSymbol[0];

            if (cache.TryGetValue(assm, out result))
            {
                return result;
            }

            this.logger.LogInformation($"Looking for MagicSymbols in {assm.Assembly.FullName}");

            var magicTypes = assm.Assembly
                .GetTypes()
                .Where(t =>
                {
                    if (!t.IsClass && t.IsAbstract) { return false; }
                    var matched = t.IsSubclassOf(typeof(MagicSymbol));
                    this.logger.LogDebug("Class {Class} subclass of MagicSymbol? {Matched}", t.FullName, matched);
                    return matched;
                });

            var allMagic = new List<MagicSymbol>();
            foreach(var t in magicTypes)
            {
                try
                {
                    var m = ActivatorUtilities.CreateInstance(services, t) as MagicSymbol;
                    allMagic.Add(m);
                    this.logger.LogInformation($"Found MagicSymbols {m.Name} ({t.FullName})");
                }
                catch (Exception e)
                {
                    this.logger.LogWarning($"Unable to create instance of MagicSymbol {t.FullName}. Magic will not be enabled.\nMessage:{e.Message}");
                }
            }

            result = allMagic.ToArray();
            cache[assm] = result;

            return result;
        }
    }
}
