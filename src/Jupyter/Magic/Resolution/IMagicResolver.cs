// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

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
    ///     Subinterface of <see cref="Microsoft.Jupyter.Core.ISymbolResolver" />
    ///     with additional functionality for discovering magic symbols.
    /// </summary>
    public interface IMagicSymbolResolver : ISymbolResolver
    {
        ISymbol? ISymbolResolver.Resolve(string symbolName) =>
            this.Resolve(symbolName);

        /// <summary>
        /// Returns the <see cref="MagicSymbol"/> corresponding to the given symbol name,
        /// searching all loaded assemblies for classes derived from <see cref="MagicSymbol"/>.
        /// </summary>
        /// <param name="symbolName">The magic symbol name to resolve.</param>
        /// <returns>The resolved <see cref="MagicSymbol"/> object, or <c>null</c> if none was found.</returns>
        public new MagicSymbol? Resolve(string symbolName);

        /// <summary>
        ///     Given a type representing an assembly, adds that assembly to the
        ///     list of assemblies to be searched for built-in magic commands.
        /// </summary>
        public IMagicSymbolResolver AddKernelAssembly<TAssembly>();

        /// <summary>
        /// Returns the list of all <see cref="MagicSymbol"/> objects defined in loaded assemblies.
        /// </summary>
        public IEnumerable<MagicSymbol> FindAllMagicSymbols();

        /// <summary>
        /// Finds the MagicSymbols inside an assembly, and returns an instance of each.
        /// </summary>
        public IEnumerable<MagicSymbol> FindMagic(AssemblyInfo assm);
    }
}
