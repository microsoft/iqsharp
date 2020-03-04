// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.Simulation.Common;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;

namespace Microsoft.Quantum.IQSharp
{
    internal struct DisplayableException
    {
        public Exception Exception;
        public IEnumerable<StackFrame> StackTrace;

        public string Header =>
            $"Unhandled exception. {Exception.GetType().FullName}: {Exception.Message}";
    }

    internal static class StackFrameExtensions
    {
        internal static string ToFriendlyName(this ICallable callable)
        {
            var fullName = callable.FullName;
            return fullName.StartsWith(Snippets.SNIPPETS_NAMESPACE)
            ? fullName.Substring(Snippets.SNIPPETS_NAMESPACE.Length + 1)
            : fullName;
        }

        internal static string ToSourceLink(this StackFrame frame) =>
            Regex.Match(frame.SourceFile, "snippet_[0-9]*.qs$").Success
            ? "(notebook)"
            : $"<a href=\"{frame.GetBestSourceLocation()}\">{frame.SourceFile}:{frame.FailedLineNumber}</a>";
    }

    public class DisplayableExceptionToHtmlEncoder : IResultEncoder
    {
        public string MimeType => MimeTypes.Html;

        public EncodedData? Encode(object displayable)
        {
            if (displayable is DisplayableException ex)
            {
                var rows = ex
                    .StackTrace
                    .Select(frame => $@"
                        <tr>
                            <td>{frame.ToSourceLink()}</td>
                            <td>{frame.Callable.ToFriendlyName()}</td>
                        </tr>
                    ");
                var table = $@"
                    <details>
                        <summary>
                            Unhandled exception of type {ex.Exception.GetType().FullName}: {ex.Exception.Message}
                        </summary>
                        <table>
                            <thead>
                                <tr>
                                    <th>Source</th>
                                    <th>Callable</th>
                                </tr>
                            </thead>

                            <tbody>
                                {String.Join("\n", rows)}
                            </tbody>
                        </table>
                    </details>
                ";
                return table.ToEncodedData();
            }
            else return null;
        }
    }
    public class DisplayableExceptionToTextEncoder : IResultEncoder
    {
        public string MimeType => MimeTypes.PlainText;

        public EncodedData? Encode(object displayable)
        {
            if (displayable is DisplayableException ex)
            {
                var builder = new StringBuilder();
                builder.AppendLine(ex.Header);
                var first = true;
                foreach (var frame in ex.StackTrace)
                {
                    builder.AppendLine(
                        (first ? " ---> " : "   at ") +
                        frame.ToStringWithBestSourceLocation()
                    );
                    first = false;
                }
                return builder.ToString().ToEncodedData();
            }
            else return null;
        }
    }

}
