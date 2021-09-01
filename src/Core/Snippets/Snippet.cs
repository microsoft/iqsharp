// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.IQSharp.Common;

using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    ///  A Snippet represents a piece of Q# code provided by the user.
    ///  These snippets are ephemeral thus not part of the environment.
    ///  Each Snippet represents a single entry from the user.
    ///  During execution, a user may provide multiple Snippets.
    /// </summary>
    public class Snippet
    {
        /// <summary>
        /// An id of the snippet. This gives users control on whether they are updating
        /// or creating a new Snippet.
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// The actual source code from the user.
        /// </summary>
        public string code { get; set; }

        /// <summary>
        ///     The name of the namespace that this snippet should be compiled
        ///     in, or <c>null</c> if the default namespace should be used.
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// Any compilation warnings trigger for this Snippet.
        /// </summary>
        public string[] warnings { get; set; }

        /// <summary>
        /// The Q# compiled version of the operations.
        /// </summary>
        [JsonIgnore]
        public QsNamespaceElement[] Elements { get; set; }

        /// <summary>
        ///     The compiler needs an actual URI for each piece of Q# code
        ///      that it is going to compile.
        /// </summary>
        [JsonIgnore]
        public Uri Uri => new Uri(Path.GetFullPath(Path.Combine("/", $"snippet_{id}.qs")));
    }
}
