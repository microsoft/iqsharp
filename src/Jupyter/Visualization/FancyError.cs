// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.IQSharp.Jupyter;

/// <summary>
///     Represents a diagnostic together with the source that generated that
///     diagnostic.
/// </summary>
public record FancyError(string? Source, Diagnostic Diagnostic)
{
    private string UnderlineColor => Diagnostic.Severity switch
    {
        DiagnosticSeverity.Error => "red",
        DiagnosticSeverity.Information => "blue",
        DiagnosticSeverity.Warning => "orange",
        DiagnosticSeverity.Hint => "green",
        _ => "black"
    };

    private static readonly Dictionary<SumType<int, string>?, Uri?> DocsUriCache = new();
    private static readonly ImmutableDictionary<SumType<int, string>, string> Hints =
        new Dictionary<SumType<int, string>, string>
        {
            ["QS5023"] =
                "The currently targeted device or target capability doesn't support comparing measurement results. " +
                "You may be able to compile and run your program on a different target by using %azure.target, or by increasing the capability level by using %azure.target-capability.",
        }
        .ToImmutableDictionary();

    /// <summary>
    ///     Returns a user-friendly hint about this diagnostic if one exists,
    ///     or <c>null</c> if no hint is available.
    /// </summary>
    public string? Hint =>
        Diagnostic.Code is {} code
        ? Hints.TryGetValue(code, out var hint)
          ? hint
          : null
        : null;

    /// <summary>
    ///     Attempts to get a link to a documentation page for this diagnostic.
    /// </summary>
    /// <returns>
    ///     A URI that resolves to a page about this diagnostic if one exists
    ///     and can be found within the timeout, or <c>null</c> if no such
    ///     page exists, or if an exception was encountered.
    /// </returns>
    public async Task<Uri?> TryGetDocumentationPage()
    {
        if (Diagnostic.Code == null)
        {
            return null;
        }

        if (DocsUriCache.TryGetValue(Diagnostic.Code, out var cachedUri))
        {
            return cachedUri;
        }

        if (!Diagnostic.Code.HasValue)
        {
            DocsUriCache[Diagnostic.Code] = null;
            return null;
        }

        const string xrefLookupBase = "https://xref.docs.microsoft.com/query?uid=microsoft.quantum.qscompiler-diagnostics.";
        var lookupUri = xrefLookupBase + (
            Diagnostic.Code.Value.TryGetFirst(out var intCode)
            ? intCode.ToString()
            : Diagnostic.Code.Value.Second
        );
        try
        {
            var handler = new HttpClientHandler();
            handler.CheckCertificateRevocationList = true;
            var client = new HttpClient(handler);
            var tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(2));
            var resp = await client.GetAsync(lookupUri, tokenSource.Token);
            if (resp.StatusCode != HttpStatusCode.OK)
            {
                DocsUriCache[Diagnostic.Code] = null;
                return null;
            }
            var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
            var xrefs = doc.RootElement.EnumerateArray().ToList();
            if (xrefs.Count == 1 && xrefs[0].TryGetProperty("href", out var href))
            {
                var uri = new Uri(href.GetString());
                DocsUriCache[Diagnostic.Code] = uri;
                return uri;
            }
            else
            {
                DocsUriCache[Diagnostic.Code] = null;
                return null;
            }
        }
        catch
        {
            DocsUriCache[Diagnostic.Code] = null;
            return null;
        }
    }

    private IEnumerable<(int? Number, string Line)> RelevantLines(int nContextLines = 1, bool html = false)
    {
        // NB: Diagnostic.Range can be null, even though its nullability
        //     metadata promises otherwise. If so, there's no relevant lines
        //     we can quote here.
        if (Diagnostic.Range is null || Source is null)
        {
            yield break;
        }

        var lines = Source.Split("\n");
        var startLine = System.Math.Max(Diagnostic.Range.Start.Line - nContextLines, 0);
        var stopLine = System.Math.Min(Diagnostic.Range.Start.Line + nContextLines + 1, lines.Count());
        foreach (var idxLine in Enumerable.Range(startLine, stopLine - startLine))
        {
            var line = lines[idxLine];

            // If highlighting a single line, make a new line of arrows
            // highlighting the specific range on that line.
            if (Diagnostic.Range.Start.Line == Diagnostic.Range.End.Line && Diagnostic.Range.Start.Line == idxLine)
            {
                if (html)
                {
                    var prefix = line.Substring(0, Diagnostic.Range.Start.Character);
                    var highlight = line.Substring(Diagnostic.Range.Start.Character, Diagnostic.Range.End.Character - Diagnostic.Range.Start.Character);
                    var postfix = line.Substring(Diagnostic.Range.End.Character);
                    var style = $"font-weight: bold; text-decoration: underline; text-decoration-style: wavy; text-decoration-color: {UnderlineColor}";
                    yield return (
                        idxLine + 1,
                        WebUtility.HtmlEncode(prefix) +
                        $"<span style=\"{style}\">" +
                        WebUtility.HtmlEncode(highlight) +
                        "</span>" +
                        WebUtility.HtmlEncode(postfix)
                    );
                }
                else
                {
                    yield return (idxLine + 1, line);
                    yield return (
                        null,
                        new string(' ', Diagnostic.Range.Start.Character) +
                        new string('^', Diagnostic.Range.End.Character - Diagnostic.Range.Start.Character)
                    );
                }
            }
            else
            {
                yield return (idxLine + 1, html ? WebUtility.HtmlEncode(line) : line);
            }
        }
    }

    /// <summary>
    ///     Returns the source for this error, annotated with information from
    ///     the diagnostic.
    /// </summary>
    /// <param name="nContextLines">
    ///      The maximum number of lines of context above and below the
    ///      diagnostic that should be included in the annotated source.
    /// </param>
    /// <param name="html">
    ///     If <c>true</c>, formats annotated source as an HTML block element.
    /// </param>
    public string AnnotatedSource(int nContextLines = 1, bool html = false)
    {
        var lines = RelevantLines(nContextLines, html).ToList();
        if (lines.Count == 0)
        {
            return "";
        }

        var lineNumWidth = lines
            .Select(line => line.Number)
            .Where(n => n.HasValue)
            .Select(n => n!.Value)
            .Max()
            .ToString()
            .Length;
        var annotatedSource = string.Join(
            "\n", lines.Select(
                line => $@" {(
                    line.Number is {} n
                    ? n.ToString().PadLeft(lineNumWidth)
                    : new string(' ', lineNumWidth)
                )} | {line.Line}"
            )
        );
        return html
            ? $"<pre><code>{annotatedSource}</code></pre>"
            : annotatedSource;
    }
}

/// <summary>
///     Encodes fancy error diagnostics into plain text suitable for display
///     at a command-line or other console.
/// </summary>
public record FancyErrorToTextEncoder(IConfigurationSource ConfigurationSource) : IResultEncoder
{
    /// <inheritdoc/>
    public string MimeType => MimeTypes.PlainText;

    /// <inheritdoc/>
    public EncodedData? Encode(object displayable)
    {
        if (displayable is FancyError error)
        {
            var annotedSource = error.AnnotatedSource(ConfigurationSource.NContextLines);
            var code = error.Diagnostic.Code is {} sumCode
                ? sumCode.Match(i => $" {i}", s => $" {s}")
                : "";
            var hint = error.Hint is {} s
                ? $"\nHint: {s}"
                : "";
            var moreInfo = error.TryGetDocumentationPage().Result is {} uri
                ? "\nFor more information, see {uri}."
                : "";
            return $"{error.Diagnostic.Severity}{code}: {error.Diagnostic.Message}\n{annotedSource}{hint}{moreInfo}".ToEncodedData();
        }
        else return null;
    }
}

/// <summary>
///     Encodes fancy error diagnostics into HTML suitable for inclusion in a
///     notebook or other rich-text interface.
/// </summary>
public record FancyErrorToHtmlEncoder(IConfigurationSource ConfigurationSource) : IResultEncoder
{
    /// <inheritdoc/>
    public string MimeType => MimeTypes.Html;

    /// <inheritdoc/>
    public EncodedData? Encode(object displayable)
    {
        if (displayable is FancyError error)
        {
            var annotedSource = error.AnnotatedSource(ConfigurationSource.NContextLines, html: true);
            var code = error.Diagnostic.Code is {} sumCode
                ? sumCode.Match(i => $"{i}", s => $"{s}")
                : "";
            var hint = error.Hint is {} s
                ? $"\n<br><small><em>Hint</em>: {s}</small>"
                : "";
            var moreInfo = error.TryGetDocumentationPage().Result is {} uri
                ? $"\n<br><small>For more information, see the <a href=\"{uri}\">Azure Quantum documentation for {code}</a></small>."
                : "";
            return $"<strong>{error.Diagnostic.Severity}{(string.IsNullOrWhiteSpace(code) ? "" : " " + code)}</strong>: {error.Diagnostic.Message}\n{annotedSource}{hint}{moreInfo}".ToEncodedData();
        }
        else return null;
    }
}
