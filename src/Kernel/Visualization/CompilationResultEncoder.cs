// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.Simulation.Simulators;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.IQSharp.Kernel
{

    internal static class LspDiagnosticExtensions
    {
        public static string ToHtml(this Diagnostic diagnostic, IDictionary<Uri, string>? sources, bool expand = true, bool showHints = true)
        {
            string? source = null;
            sources?.TryGetValue(new Uri(diagnostic.Source), out source);
            var body = "";
            if (source != null)
            {
                var lines = source
                    .Split(new string[] { "\n", "\r\n" }, StringSplitOptions.None)
                    // Our logger is configured to skip the first line, so do so here as well.
                    .Skip(1)
                    // We also need to omit the trailing } added by our wrappers.
                    .SkipLast(1)
                    .ToArray();
                var relevantLines = lines[diagnostic.Range.Start.Line..(diagnostic.Range.End.Line + 1)];
                // System.Diagnostics.Debugger.Launch();

                var prefixLines = lines.Take(diagnostic.Range.Start.Line).ToList();
                prefixLines.Add(relevantLines[0].Substring(0, diagnostic.Range.Start.Character));
                relevantLines[0] = relevantLines[0].Substring(diagnostic.Range.Start.Character);

                var suffixLines = lines.Skip(1 + diagnostic.Range.End.Line).ToList();
                var splitSuffixAt = diagnostic.Range.Start.Line == diagnostic.Range.End.Line
                    ? diagnostic.Range.End.Character - diagnostic.Range.Start.Character
                    : diagnostic.Range.End.Character;
                suffixLines = suffixLines.Prepend(relevantLines[^1].Substring(splitSuffixAt)).ToList();
                relevantLines[^1] = relevantLines[^1].Substring(0, splitSuffixAt);

                body = $@"
                    <pre><code>{
                        string.Join("\n", prefixLines)
                    }<span style=""font-weight: bold; text-decoration: underline wavy {(diagnostic.Severity switch
                    {
                        DiagnosticSeverity.Error => "red",
                        DiagnosticSeverity.Warning => "orange",
                        DiagnosticSeverity.Information => "green",
                        DiagnosticSeverity.Hint => "blue",
                        _ => "red"
                    })};"">{
                        string.Join("\n", relevantLines)
                    }</span>{
                        string.Join("\n", suffixLines)
                    }</code></pre>
                ";
            }

            var hint = showHints
                       && diagnostic.Code != null
                       && ErrorCodes.AdditionalInformation.TryGetValue(diagnostic.Code, out var info)
                       ? $"<span style=\"font-size: 10px;\">{Markdig.Markdown.ToHtml(info.Hint)}</span><br>"
                       : "";

            var severity = diagnostic.Severity is DiagnosticSeverity level
                           ? $"{level.ToString().ToLower()} "
                           : "";
            return $@"
                <details{(expand ? " open" : "")}>
                    <summary>
                        <strong>{severity}{diagnostic.Code}:</strong>
                        {diagnostic.Message}
                    </summary>
                    {body}
                    {hint}
                </details>
            ";
        }
    }

    internal record ErrorCodeInformation(string Hint);
    internal static class ErrorCodes
    {
        internal static class Links
        {
            internal const string ApiReference = "https://docs.microsoft.com/qsharp/api/qsharp";
            internal const string LanguageGuide = "https://docs.microsoft.com/azure/quantum/user-guide/language";
        }

        private static readonly string AboutUseStatement = $"For more information about the new `use` and `borrow` statements, check out <{Links.LanguageGuide}/statements/quantummemorymanagement#use-statement>.";

        internal static readonly ImmutableDictionary<string, ErrorCodeInformation> AdditionalInformation =
            new Dictionary<string, ErrorCodeInformation>
            {
                ["QS0001"] = new ErrorCodeInformation(
                    Hint: $"When calling functions and operations, the types of inputs must match exactly. For information about the types taken as inputs by functions and operations in the Q# standard library, check out the API reference at <{Links.ApiReference}>. For more information about types in Q#, please see <{Links.LanguageGuide}/typesystem>."
                ),
                ["QS3036"] = new ErrorCodeInformation(
                    Hint: $"For a list of different kinds of statements in Q#, please see <{Links.LanguageGuide}/statements>."
                ),
                ["QS3306"] = new ErrorCodeInformation(
                    Hint: AboutUseStatement
                ),
                ["QS3307"] = new ErrorCodeInformation(
                    Hint: AboutUseStatement
                ),
                ["QS5022"] = new ErrorCodeInformation(
                    Hint: $"You may need to `open` an additional namespace to use this function or operation. Check <{Links.ApiReference}> for a listing of all namespaces available in the Q# standard libraries."
                ),
                ["QS6005"] = new ErrorCodeInformation(
                    Hint: $"If you meant to use a user-defined type, you may need to `open` an additional namespace to do so. Check <{Links.ApiReference}> for a listing of all namespaces available in the Q# standard libraries."
                ),
                ["QS6314"] = new ErrorCodeInformation(
                    Hint: $"In operations with automatically generated adjoints (for example, operations declared with `is Adj` and no explicit adjoint body), the `set` statement cannot be used. For more about automatically generated adjoint specializations, please see <{Links.ApiReference}/programstructure/specializationdeclarations#auto-generation-directives>."
                )
            }
            .ToImmutableDictionary();
    }

    /// <summary>
    ///     Encodes results from the <c>%lsmagic</c> magic command as an HTML
    ///     table.
    /// </summary>
    public class CompilationResultToHtmlEncoder : IResultEncoder
    {
        private IConfigurationSource configurationSource;
        public CompilationResultToHtmlEncoder(IConfigurationSource configurationSource)
        {
            this.configurationSource = configurationSource;
        }

        /// <inheritdoc />
        public string MimeType => MimeTypes.Html;

        /// <summary>
        ///     Checks if a displayable object represents a list of magic symbol
        ///     summaries, and if so, returns an encoding into an HTML table.
        /// </summary>
        public EncodedData? Encode(object displayable)
        {
            if (displayable is not IQSharpEngine.CompilationResult result)
            {
                return null;
            }

            var output = "";
            if (result.NewDeclarations is {} newDeclarations)
            {
                output += $@"
                    <h4>Compiled declarations:</h4>
                    <ul>
                    {
                        string.Join("\n",
                            newDeclarations.Select(decl =>
                                $"<li>{decl}</li>"
                            )
                        )
                    }
                    </ul>
                ";
            }

            if (result.Diagnostics.Any())
            {
                output += $@"
                    <h4>Errors and warnings:</h4>
                    <ul>
                    {
                        string.Join("\n",
                            result.Diagnostics.Select(diagnostic =>
                                diagnostic.ToHtml(
                                    result.Sources,
                                    expand: configurationSource.ExpandErrorMessages,
                                    showHints: configurationSource.ShowErrorHints
                                )
                            )
                        )
                    }
                    </ul>
                ";
            }

            return output.ToEncodedData();
        }
    }

}
