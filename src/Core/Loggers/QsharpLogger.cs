// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

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
        public ILogger Logger { get; }

        public List<LSP.Diagnostic> Logs { get; }

        public QSharpLogger(ILogger logger)
        {
            this.Logger = logger;
            this.Logs = new List<LSP.Diagnostic>();
        }

        public static LogLevel MapLevel(LSP.DiagnosticSeverity original)
        {
            switch (original)
            {
                case LSP.DiagnosticSeverity.Error:
                    return LogLevel.Error;
                case LSP.DiagnosticSeverity.Warning:
                    return LogLevel.Warning;
                case LSP.DiagnosticSeverity.Information:
                    return LogLevel.Information;
                case LSP.DiagnosticSeverity.Hint:
                    return LogLevel.Debug;
                default:
                    return LogLevel.Trace;
            }
        }

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

        public IEnumerable<string> ErrorIds =>
            Logs
                .Where(m => m.Severity == VisualStudio.LanguageServer.Protocol.DiagnosticSeverity.Error)
                .Select(m => m.Code);

        protected override void Print(LSP.Diagnostic m)
        {
            Logger?.Log(MapLevel(m.Severity), $"{m.Code}: {m.Message}");
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
