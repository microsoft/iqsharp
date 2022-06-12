// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;

using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp.Jupyter
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
        public string Source => Operation.Header.Source.AssemblyOrCodeFile;

        /// <summary>
        ///     The documentation for this symbol, as provided by its API
        ///     documentation comments.
        /// </summary>
        [JsonProperty("documentation")]
        public string Documentation => String.Join("\n", Operation.Header.Documentation);

        /// <summary>
        ///      A short summary of the given symbol, as provided by its API
        ///      documentation comments.
        /// </summary>
        [JsonProperty("summary", NullValueHandling=NullValueHandling.Ignore)]
        public string? Summary { get; private set; } = null;        

        /// <summary>
        ///      An extended description of the given symbol, as provided by its API
        ///      documentation comments.
        /// </summary>
        [JsonProperty("description", NullValueHandling=NullValueHandling.Ignore)]
        public string? Description { get; private set; } = null;

        /// <summary>
        /// </summary>
        [JsonProperty("inputs", NullValueHandling=NullValueHandling.Ignore)]
        public ImmutableDictionary<string?, string?> Inputs { get; private set; }

        /// <summary>
        /// </summary>
        [JsonProperty("examples", NullValueHandling=NullValueHandling.Ignore)]
        public ImmutableList<string?> Examples { get; private set; }

        
        [JsonProperty("type_parameters", NullValueHandling=NullValueHandling.Ignore)]
        public ImmutableDictionary<string?, string?> TypeParameters { get; private set; }

        // TODO: continue exposing documentation here.

        /// <summary>
        ///     Constructs a new symbol given information about an operation
        ///     as provided by the Q# compiler.
        /// </summary>
        public IQSharpSymbol(OperationInfo op)
        {
            if (op == null) { throw new ArgumentNullException(nameof(op)); }
            this.Operation = op;
            this.Kind = SymbolKind.Other;

            this.Summary = this
                .Operation
                .GetStringAttributes("Summary")
                .SingleOrDefault();
            this.Description = this
                .Operation
                .GetStringAttributes("Description")
                .SingleOrDefault();
            this.Inputs = this
                .Operation
                .GetDictionaryAttributes("Input")
                .ToImmutableDictionary();
            this.TypeParameters = this
                .Operation
                .GetDictionaryAttributes("TypeParameter")
                .ToImmutableDictionary();
            this.Examples = this
                .Operation
                .GetStringAttributes("Example")
                .Where(ex => ex != null)
                .ToImmutableList();
            
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
        /// <param name="eventService">
        ///     The event service used to signal the successful start of this
        ///     resolver service.
        /// </param>
        public SymbolResolver(IOperationResolver opsResolver, IEventService eventService)
        {
            this.opsResolver = opsResolver;

            eventService?.TriggerServiceInitialized<ISymbolResolver>(this);
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
        public ISymbol? Resolve(string symbolName)
        {
            var op = opsResolver.Resolve(symbolName);
            return op == null ? null : new IQSharpSymbol(op);
        }
    }
}
