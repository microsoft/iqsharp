// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Collections.Generic;
using Microsoft.Jupyter.Core;
using Markdig;
using Microsoft.Quantum.IQSharp.Jupyter;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using System;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    // NB: These are defined in the documentation generation tool in the
    //     compiler, and should not be duplicated here. These should be removed
    //     before merging to main.
    internal static class SyntaxExtensions
    {
        internal static List<(string, ResolvedType)> InputDeclarations(this QsTuple<LocalVariableDeclaration<QsLocalSymbol>> items) => items switch
            {
                QsTuple<LocalVariableDeclaration<QsLocalSymbol>>.QsTuple tuple =>
                    tuple.Item.SelectMany(
                        item => item.InputDeclarations())
                    .ToList(),
                QsTuple<LocalVariableDeclaration<QsLocalSymbol>>.QsTupleItem item =>
                    new List<(string, ResolvedType)>
                    {
                        (
                            item.Item.VariableName switch
                            {
                                QsLocalSymbol.ValidName name => name.Item,
                                _ => "__invalid__",
                            },
                            item.Item.Type),
                    },
                _ => throw new Exception(),
            };

        internal static string ToSyntax(this ResolvedCharacteristics characteristics) =>
            characteristics.SupportedFunctors switch
            {
                { IsNull: true } => "",
                { Item: { Count: 0 } } => "",

                // Be sure to add the leading space before is!
                { Item: var functors } => $" is {string.Join(" + ", functors.Select(functor => functor.ToSyntax()))}",
            };

        internal static string ToSyntax(this QsFunctor functor) =>
            functor.Tag switch
            {
                QsFunctor.Tags.Adjoint => "Adj",
                QsFunctor.Tags.Controlled => "Ctl",
                _ => "__invalid__",
            };

        // TODO: memoize
        internal async static Task<string?> TryResolveXref(string xref)
        {
            var client = new HttpClient();
            try
            {
                var response = await client.GetStringAsync($"https://xref.docs.microsoft.com/query?uid={xref}");
                var json = JToken.Parse(response);
                return json.Value<string>("href");
            }
            catch
            {
                return null;
            }
        }

        internal async static Task<string> ToLink(string text, string xref, string? fragment = null)
        {
            var href = await TryResolveXref(xref);
            if (href == null)
            {
                return text;
            }
            else
            {
                return $"<a href=\"{href}{(fragment == null ? "" : $"#{fragment}")}</a>";
            }
        }

        internal static async Task<string> ToHtml(this ResolvedType type) => type.Resolution switch
            {
                ResolvedTypeKind.ArrayType array => $"{await array.Item.ToHtml()}[]",
                ResolvedTypeKind.Function function =>
                    $"{await function.Item1.ToHtml()} -> {await function.Item2.ToHtml()}",
                ResolvedTypeKind.Operation operation =>
                    $"{await operation.Item1.Item1.ToHtml()} => {await operation.Item1.Item2.ToHtml()} "
                    + operation.Item2.Characteristics.ToSyntax(),
                ResolvedTypeKind.TupleType tuple => "(" + string.Join(
                    ",", tuple.Item.Select(async type => await type.ToHtml())) + ")",
                ResolvedTypeKind.UserDefinedType udt => await udt.Item.ToHtml(),
                ResolvedTypeKind.TypeParameter typeParam =>
                    $"'{typeParam.Item.TypeName}",
                _ => type.Resolution.Tag switch
                    {
                        ResolvedTypeKind.Tags.BigInt => await ToLink("BigInt", "xref:microsoft.quantum.qsharp.valueliterals", "bigint-literals"),
                        ResolvedTypeKind.Tags.Bool => await ToLink("Bool", "xref:microsoft.quantum.qsharp.valueliterals", "bool-literals"),
                        ResolvedTypeKind.Tags.Double => await ToLink("Double", "xref:microsoft.quantum.qsharp.valueliterals", "double-literals"),
                        ResolvedTypeKind.Tags.Int => await ToLink("Int", "xref:microsoft.quantum.qsharp.valueliterals", "int-literals"),
                        ResolvedTypeKind.Tags.Pauli => await ToLink("Pauli", "xref:microsoft.quantum.qsharp.valueliterals", "pauli-literals"),
                        ResolvedTypeKind.Tags.Qubit => await ToLink("Qubit", "xref:microsoft.quantum.qsharp.valueliterals", "qubit-literals"),
                        ResolvedTypeKind.Tags.Range => await ToLink("Range", "xref:microsoft.quantum.qsharp.valueliterals", "range-literals"),
                        ResolvedTypeKind.Tags.String => await ToLink("String", "xref:microsoft.quantum.qsharp.valueliterals", "string-literals"),
                        ResolvedTypeKind.Tags.UnitType => await ToLink("Unit", "xref:microsoft.quantum.qsharp.valueliterals", "unit-literal"),
                        ResolvedTypeKind.Tags.Result => await ToLink("Result", "xref:microsoft.quantum.qsharp.valueliterals", "result-literal"),
                        ResolvedTypeKind.Tags.InvalidType => "__invalid__",
                        _ => $"__invalid<{type.Resolution.ToString()}>__",
                    },
            };

        internal static async Task<string> ToHtml(this UserDefinedType type) =>
            await ToLink($"{type.Namespace}.{type.Name}", $"{type.Namespace}.{type.Name}");
    }

    /// <summary>
    ///     Encodes Q# symbols into plain text, e.g. for printing to the console.
    /// </summary>
    public class IQSharpSymbolToTextResultEncoder : IResultEncoder
    {
        /// <inheritdoc />
        public string MimeType => MimeTypes.PlainText;

        /// <summary>
        ///     Checks if a displayable object is an IQ# symbol, and if so,
        ///     returns an encoding of that symbol into plain text.
        /// </summary>
        public EncodedData? Encode(object displayable)
        {
            if (displayable is IQSharpSymbol symbol)
            {
                // TODO: display documentation here.
                //       We will need to parse the documentation to get out the summary, though.
                return $"{symbol.Name}".ToEncodedData();
            }
            else return null;
        }
    }

    /// <summary>
    ///      Encodes Q# symbols into HTML for display in Jupyter Notebooks and
    ///      other similar interfaces.
    /// </summary>
    public class IQSharpSymbolToHtmlResultEncoder : IResultEncoder
    {
        private readonly IConfigurationSource ConfigurationSource;

        /// <inheritdoc />
        public string MimeType => MimeTypes.Html;

        public IQSharpSymbolToHtmlResultEncoder(IConfigurationSource configurationSource)
        {
            this.ConfigurationSource = configurationSource;
        }

        /// <summary>
        ///     Checks if a displayable object is an IQ# symbol, and if so,
        ///     returns an encoding of that symbol into HTML.
        /// </summary>
        public EncodedData? Encode(object displayable)
        {
            var tableEncoder = new TableToHtmlDisplayEncoder();

            if (displayable is IQSharpSymbol symbol)
            {
                var codeLink =
                    $"<a href=\"{symbol.Source}\"><i class=\"fa fas fa-code\"></i></a>";
                var summary = symbol.Summary != null
                    ? "<h5>Summary</h5>" + Markdown.ToHtml(symbol.Summary)
                    : string.Empty;
                var description = symbol.Description != null
                    ? "<h5>Description</h5>" + Markdown.ToHtml(symbol.Description)
                    : string.Empty;
                // TODO: Make sure to list
                //       type parameters even if they're not documented.
                var typeParams = symbol.TypeParameters.Count > 0
                    ? "<h5>Type Parameters</h5>\n" +
                      tableEncoder.Encode(new Table<KeyValuePair<string?, string?>>
                      {
                          Columns = new List<(string, Func<KeyValuePair<string?, string?>, string>)>
                          {
                              ("", input => $"<code>{input.Key}</code>"),
                              ("", input => Markdown.ToHtml(input.Value))
                          },
                          Rows = symbol.TypeParameters.ToList()
                      })!.Value.Data
                    : string.Empty;

                // TODO: Check if Inputs is empty before formatting, make sure
                //       to list even if they're not documented.
                var inputDecls = symbol.Operation.Header.ArgumentTuple.InputDeclarations().ToDictionary(item => item.Item1, item => item.Item2);
                var inputs = symbol.Inputs.Count > 0
                    ? "<h5>Inputs</h5>\n" + tableEncoder.Encode(new Table<KeyValuePair<string?, string?>>
                    {
                        Columns = new List<(string, Func<KeyValuePair<string?, string?>, string>)>
                        {
                            ("", input => $"<code>{input.Key}</code>"),
                            ("", input => $"<code>{inputDecls[input.Key].ToHtml().Result}</code>"),
                            ("", input => Markdown.ToHtml(input.Value))
                        },
                        Rows = symbol.Inputs.ToList()
                    })!.Value.Data
                    : string.Empty;
                var examples = string.Join("\n",
                    symbol.Examples.Select(example => $"<h5>Example</h5>\n{Markdown.ToHtml(example)}")
                );

                var attributes = ConfigurationSource.InternalHelpShowAllAttributes
                    ? tableEncoder.Encode(new Table<QsDeclarationAttribute>
                      {
                          Columns = new List<(string, Func<QsDeclarationAttribute, string>)>
                          {
                              ("Name", attr => attr.TypeId switch
                              {
                                  { Item: UserDefinedType udt } => $"{udt.Namespace}.{udt.Name}",
                                  _ => "<unknown>"
                              }),
                              ("Value", attr => attr.Argument.ToString())
                          },
                          Rows = symbol.Operation.Header.Attributes.ToList()
                      })!.Value.Data
                    : "";
                return $@"
                    <h4><i class=""fa fas fa-terminal""></i> {symbol.Name} {codeLink}</h4>
                    {summary}
                    {description}
                    {typeParams}
                    {inputs}
                    {examples}
                    {attributes}
                ".ToEncodedData();

            }
            else return null;
        }
    }

}
