// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System.IO;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

using Newtonsoft.Json;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.IQSharp;

/// <summary>
///  A Snippet represents a piece of Q# code provided by the user.
///  These snippets are ephemeral thus not part of the environment.
///  Each Snippet represents a single entry from the user.
///  During execution, a user may provide multiple Snippets.
/// </summary>
public record Snippet
{
    /// <summary>
    /// An id of the snippet. This gives users control on whether they are updating
    /// or creating a new Snippet.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// The actual source code from the user.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Any compilation warnings trigger for this Snippet.
    /// </summary>
    public string[]? Warnings { get; init; }

    [Obsolete("Please use Snippet.Warnings.")]
    public string[]? warnings => Warnings;

    public IEnumerable<Diagnostic>? Diagnostics { get; init; }

    /// <summary>
    /// The Q# compiled version of the operations.
    /// </summary>
    [JsonIgnore]
    public QsNamespaceElement[]? Elements { get; set; }

    [JsonIgnore]
    internal string? FileName => Path.GetFullPath(Path.Combine("/", $"snippet_{Id}.qs"));

    /// <summary>
    ///     The compiler needs an actual URI for each piece of Q# code
    ///      that it is going to compile.
    /// </summary>
    [JsonIgnore]
    public Uri Uri => new Uri(FileName);
}
