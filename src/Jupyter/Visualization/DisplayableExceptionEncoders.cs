// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable

using Microsoft.Quantum.Simulation.Common;
using Microsoft.Quantum.Simulation.Core;
using System.Data;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Quantum.IQSharp
{
    internal record struct DisplayableStackFrame(string Callable, string SourceFile, string BestSourceLocation, int LineNumber)
    {
    }

    internal record struct DisplayableException(string? ExceptionType, string ExceptionMessage, IEnumerable<DisplayableStackFrame> StackTrace)
    {
        public static DisplayableException Create(Exception exception, IEnumerable<StackFrame> stackTrace)
        {
            return new DisplayableException
            {
                ExceptionType = exception.GetType().FullName,
                ExceptionMessage = exception.Message,
                StackTrace = stackTrace.Select(sf => new DisplayableStackFrame
                {
                    Callable = GetCallableFriendlyName(sf.Callable),
                    SourceFile = sf.SourceFile,
                    BestSourceLocation = sf.GetBestSourceLocation(),
                    LineNumber = sf.FailedLineNumber
                })
            };
        }

        public string Header =>
            $"Unhandled exception. {ExceptionType}: {ExceptionMessage}";

        private static string GetCallableFriendlyName(ICallable callable)
        {
            var fullName = callable.FullName;
            return fullName.StartsWith(Snippets.SNIPPETS_NAMESPACE)
            ? fullName.Substring(Snippets.SNIPPETS_NAMESPACE.Length + 1)
            : fullName;
        }
    }

    /// <summary>
    ///      Encodes exceptions augmented with Q# metadata into HTML tables.
    /// </summary>
    public class DisplayableExceptionToHtmlEncoder : IResultEncoder
    {
        /// <inheritdoc />
        public string MimeType => MimeTypes.Html;

        /// <inheritdoc />
        public EncodedData? Encode(object displayable)
        {
            if (displayable is DisplayableException ex)
            {
                // StackTrace should have been populated by the StackTraceCollector 
                // upon the failure event raised by the simulator
                System.Diagnostics.Debug.Assert(ex.StackTrace != null);

                var rows = ex
                    .StackTrace
                    .Select(frame => $@"
                        <tr>
                            <td>{ToSourceLink(frame)}</td>
                            <td>{frame.Callable}</td>
                        </tr>
                    ");

                var stackTrace = $@"
                    <thead>
                        <tr>
                            <th>Source</th>
                            <th>Callable</th>
                        </tr>
                    </thead>

                    <tbody>
                        {String.Join("\n", rows)}
                    </tbody>
                ";

                // { display: list-item } style is needed for a <summary> tag so that the arrow shows up 
                var table = $@"
                    <details>
                        <summary style=""display:list-item"">
                            {WebUtility.HtmlEncode(ex.Header)}
                        </summary>
                        <table>
                            {stackTrace}
                        </table>
                    </details>
                ";
                return table.ToEncodedData();
            }
            else return null;
        }

        private static string ToSourceLink(DisplayableStackFrame frame) =>
            Regex.Match(frame.SourceFile, "snippet_[0-9]*.qs$").Success
            ? "(notebook)"
            : $"<a href=\"{frame.BestSourceLocation}\">{WebUtility.HtmlEncode(frame.SourceFile)}:{frame.LineNumber}</a>";
    }

    /// <summary>
    ///      Encodes exceptions augmented with Q# metadata into plain text
    ///      tables.
    /// </summary>
    public class DisplayableExceptionToTextEncoder : IResultEncoder
    {
        /// <inheritdoc />
        public string MimeType => MimeTypes.PlainText;

        /// <inheritdoc />
        public EncodedData? Encode(object displayable)
        {
            if (displayable is DisplayableException ex)
            {
                // StackTrace should have been populated by the StackTraceCollector 
                // upon the failure event raised by the simulator
                System.Diagnostics.Debug.Assert(ex.StackTrace != null);

                var builder = new StringBuilder();
                builder.AppendLine(ex.Header);
                var first = true;
                foreach (var frame in ex.StackTrace)
                {
                    builder.AppendLine(
                        (first ? " ---> " : "   at ") +
                        $"{frame.Callable} on {frame.BestSourceLocation}:line {frame.LineNumber}"
                    );
                    first = false;
                }
                return builder.ToString().ToEncodedData();
            }
            else return null;
        }
    }

}
