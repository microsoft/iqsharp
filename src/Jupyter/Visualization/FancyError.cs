// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.IQSharp.Jupyter;

public record FancyError(string Source, Diagnostic Diagnostic)
{
    private string UnderlineColor => Diagnostic.Severity switch
    {
        DiagnosticSeverity.Error => "red",
        DiagnosticSeverity.Information => "blue",
        DiagnosticSeverity.Warning => "orange",
        DiagnosticSeverity.Hint => "black"
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

    public string? Hint =>
        Diagnostic.Code is {} code
        ? Hints.TryGetValue(code, out var hint)
          ? hint
          : null
        : null;

    public async Task<Uri?> TryGetDocumentationPage()
    {
        if (DocsUriCache.TryGetValue(Diagnostic.Code, out var cachedUri))
        {
            return cachedUri;
        }

        if (!Diagnostic.Code.HasValue)
        {
            DocsUriCache[Diagnostic.Code] = null;
            return null;
        }

        // const string xrefLookupBase = "https://xref.docs.microsoft.com/query?uid=microsoft.quantum.qscompiler-diagnstics.";
        // var lookupUri = xrefLookupBase + (
        //     Diagnostic.Code.Value.TryGetFirst(out var intCode)
        //     ? intCode.ToString()
        //     : Diagnostic.Code.Value.Second
        // );
        // Cheat...
        var lookupUri = "https://xref.docs.microsoft.com/query?uid=System.String";
        try
        {
            var client = new HttpClient();
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

    public IEnumerable<(int? Number, string Line)> RelevantLines(int nContextLines = 1, bool html = false)
    {
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
                    var style = $"text-decoration: underline; text-decoration-style: wavy; text-decoration-color: {UnderlineColor}";
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
}

public class FancyErrorToTextEncoder : IResultEncoder
{
    public string MimeType => MimeTypes.PlainText;

    public EncodedData? Encode(object displayable)
    {
        if (displayable is FancyError error)
        {
            var lines = error.RelevantLines();
            var lineNumWidth = lines
                .Select(line => line.Number)
                .Where(n => n.HasValue)
                .Select(n => n.Value)
                .Max()
                .ToString()
                .Length;
            var annotedSource = string.Join(
                "\n", lines.Select(
                    line => $@" {(
                        line.Number is {} n
                        ? n.ToString().PadLeft(lineNumWidth)
                        : new string(' ', lineNumWidth)
                    )} | {line.Line}"
                )
            );
            var code = error.Diagnostic.Code is {} sumCode
                ? sumCode.TryGetFirst(out var intCode)
                  ? $" {intCode}"
                  : $" {sumCode.Second}"
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

public class FancyErrorToHtmlEncoder : IResultEncoder
{
    public string MimeType => MimeTypes.Html;

    public EncodedData? Encode(object displayable)
    {
        if (displayable is FancyError error)
        {
            var lines = error.RelevantLines(html: true);
            var lineNumWidth = lines
                .Select(line => line.Number)
                .Where(n => n.HasValue)
                .Select(n => n.Value)
                .Max()
                .ToString()
                .Length;
            var annotedSource = string.Join(
                "\n", lines.Select(
                    line => $@" {(
                        line.Number is {} n
                        ? n.ToString().PadLeft(lineNumWidth)
                        : new string(' ', lineNumWidth)
                    )} | {line.Line}"
                )
            );
            var code = error.Diagnostic.Code is {} sumCode
                ? sumCode.TryGetFirst(out var intCode)
                  ? $"{intCode}"
                  : $"{sumCode.Second}"
                : "";
            var hint = error.Hint is {} s
                ? $"\n<br><small><em>Hint</em>: {s}</small>"
                : "";
            var moreInfo = error.TryGetDocumentationPage().Result is {} uri
                ? $"\n<br><small>For more information, see the <a href=\"{uri}\">Azure Quantum documentation for {code}</a></small>."
                : "";
            return $"<strong>{error.Diagnostic.Severity}{(string.IsNullOrWhiteSpace(code) ? "" : " " + code)}</strong>: {error.Diagnostic.Message}\n<pre><code>{annotedSource}</code></pre>{hint}{moreInfo}".ToEncodedData();
        }
        else return null;
    }
}
