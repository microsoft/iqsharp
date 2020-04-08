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
    ///     Subinterface of <see cref="Microsoft.Jupyter.Core.ISymbolResolver" />
    ///     with additional functionality for discovering magic symbols.
    /// </summary>
    public interface IMagicSymbolResolver : ISymbolResolver
    {
        ISymbol ISymbolResolver.Resolve(string symbolName) =>
            this.Resolve(symbolName);
        public new MagicSymbol Resolve(string symbolName);

        public IEnumerable<MagicSymbol> FindAllMagicSymbols();
    }
}
