// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;

using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    ///     Represents a Q# code symbol (e.g. a function or operation name)
    ///     that can be used for documentation requests (`?`) or as a completion
    ///     suggestion.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class IQSharpSymbol : ISymbol
    {
        /// <summary>
        ///     The information provided by the compiler about the operation
        ///     represented by this symbol.
        /// </summary>
        [JsonIgnore]
        public OperationInfo Operation { get; }

        /// <summary>
        ///     The name of the operation represented by this symbol.
        /// </summary>
        [JsonProperty("name")]
        public string Name => Operation.FullName;

        // TODO: serialize as stringenum.
        /// <inheritdoc />
        [JsonProperty("kind")]
        public SymbolKind Kind { get; private set; }

        /// <summary>
        ///     The source file in which the operation represented by this
        ///     symbol was defined.
        /// </summary>
        [JsonProperty("source")]
        public string Source => Operation.Header.SourceFile.Value;

        /// <summary>
        ///     The documentation for this symbol, as provided by its API
        ///     documentation comments.
        /// </summary>
        [JsonProperty("documentation")]
        public string Documentation => String.Join("\n", Operation.Header.Documentation);

        // TODO: expose documentation here.

        /// <summary>
        ///     Constructs a new symbol given information about an operation
        ///     as provided by the Q# compiler.
        /// </summary>
        public IQSharpSymbol(OperationInfo op)
        {
            if (op == null) { throw new ArgumentNullException(nameof(op)); }
            this.Operation = op;
            this.Kind = SymbolKind.Other;
        }
    }

    /// <summary>
    ///     Resolves Q# symbols into Jupyter Core symbols to be used for
    ///     documentation requests.
    /// </summary>
    public class SymbolResolver : ISymbolResolver
    {
        private readonly IOperationResolver opsResolver;

        /// <summary>
        ///     Constructs a new resolver that looks for symbols in a given
        ///     collection of snippets.
        /// </summary>
        /// <param name="opsResolver">
        ///     An object to be used to resolve operation names to symbols.
        /// </param>
        public SymbolResolver(IOperationResolver opsResolver)
        {
            this.opsResolver = opsResolver;
        }

        /// <summary>
        /// Creates a SymbolResolver from a Snippets implementation. Only used for testing.
        /// </summary>
        internal SymbolResolver(Snippets snippets)
        {
            this.opsResolver = new OperationResolver(snippets, snippets.Workspace, snippets.GlobalReferences);
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
            var op = opsResolver.Resolve(symbolName);
            return op == null ? null : new IQSharpSymbol(op);
        }
    }
}
