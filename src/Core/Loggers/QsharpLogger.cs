// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;


namespace Microsoft.Quantum.IQSharp.Common
{
    /// <summary>
    /// This class is used to call the Q# compiler and:
    ///   1. Delegate any messages to the .net core Logger
    ///   2. Keep track of the messages so we can detect if there were any errors
    /// </summary>
    public class QSharpLogger : QsCompiler.Diagnostics.LogTracker
    {
        public ILogger? Logger { get; }

        public List<LSP.Diagnostic> Logs { get; }

        public List<QsCompiler.Diagnostics.ErrorCode> ErrorCodesToIgnore { get; } = new List<QsCompiler.Diagnostics.ErrorCode>();
        public List<QsCompiler.Diagnostics.WarningCode> WarningCodesToIgnore { get; } = new List<QsCompiler.Diagnostics.WarningCode>();

        public QSharpLogger(ILogger? logger, int lineNrOffset = 0) :
            base(lineNrOffset : lineNrOffset)
        {
            this.Logger = logger;
            this.Logs = new List<LSP.Diagnostic>();
        }

        // NB: We define both a method which takes a
        //     Nullable<DiagnosticSeverity> and a plain DiagnosticSeverity
        //     value directly, as different versions of the LSP client API
        //     vary in their handling of nullablity of diagnostic severity.
        //     This allows IQ# to build with both different versions.
        //     At some point, the non-nullable overload should be removed.
        public static LogLevel MapLevel(LSP.DiagnosticSeverity? original) =>
            original switch
            {
                LSP.DiagnosticSeverity.Error => LogLevel.Error,
                LSP.DiagnosticSeverity.Warning => LogLevel.Warning,
                LSP.DiagnosticSeverity.Information => LogLevel.Information,
                LSP.DiagnosticSeverity.Hint => LogLevel.Debug,
                _ => LogLevel.Trace
            };

        public static LogLevel MapLevel(LSP.DiagnosticSeverity original) =>
            MapLevel((LSP.DiagnosticSeverity?)original);

        public bool HasErrors =>
            Logs
                .Exists(m => m.Severity == LSP.DiagnosticSeverity.Error);

        public System.Func<LSP.Diagnostic, string> Format =>
            QsCompiler.Diagnostics.Formatting.MsBuildFormat;

        public virtual IEnumerable<string> Messages =>
            this.Logs.Select(Format);

        public IEnumerable<string> Errors =>
            Logs
                .Where(m => m.Severity == LSP.DiagnosticSeverity.Error)
                .Select(Format);

        public IEnumerable<string> Warnings =>
            Logs
                .Where(m => m.Severity == LSP.DiagnosticSeverity.Warning)
                .Select(Format);

        public IEnumerable<string?> ErrorIds =>
            Logs
                .Where(m => m.Severity == VisualStudio.LanguageServer.Protocol.DiagnosticSeverity.Error && m.Code?.Second != null)
                .Select(m => m.Code?.Second);

        protected override void Print(LSP.Diagnostic m)
        {
            string diagnosticCode = m.Code?.Second ?? "";
            if (m.IsError() && ErrorCodesToIgnore.Any(code => diagnosticCode == QsCompiler.CompilationBuilder.Errors.Code(code))) return;
            if (m.IsWarning() && WarningCodesToIgnore.Any(code => diagnosticCode == QsCompiler.CompilationBuilder.Warnings.Code(code))) return;

            Logger?.Log(MapLevel(m.Severity), "{Code} ({Source}:{Range}): {Message}", diagnosticCode, m.Source, m.Range, m.Message);
            Logs.Add(m);
        }

        public virtual void LogInfo(string message)
        {
            Log(new LSP.Diagnostic
            {
                Severity = LSP.DiagnosticSeverity.Information,
                Message = message
            });
        }

        public virtual void LogDebug(string message)
        {
            Log(new LSP.Diagnostic
            {
                Severity = LSP.DiagnosticSeverity.Hint,
                Message = message
            });
        }

        public virtual void LogWarning(string code, string message)
        {
            Log(new LSP.Diagnostic
            {
                Code = code,
                Severity = LSP.DiagnosticSeverity.Warning,
                Message = message
            });
        }

        public virtual void LogError(string code, string message)
        {
            Log(new LSP.Diagnostic
            {
                Code = code,
                Severity = LSP.DiagnosticSeverity.Error,
                Message = message
            });
        }

        public void Reset()
        {
            this.Logs.Clear();
        }
    }
}
