using System;
using System.Collections.Generic;
using System.Text;

namespace MangaLib
{
    public enum DiagnosticSeverity : byte { Message, Warning, Error }
    public enum LogLevel : byte { None = 0, Errors, ErrorsAndWarnings, Verbose }
    public readonly struct Diagnostic
    {
        public readonly string Message;
        public readonly string? MethodName;
        public readonly DiagnosticSeverity Severity;
        public Diagnostic(string message, DiagnosticSeverity severity)
         : this(null, message, severity) { }
        public Diagnostic(string? methodName, string message, DiagnosticSeverity severity)
        {
            this.Message = message;
            this.Severity = severity;
            this.MethodName = methodName;
        }
        public override string ToString()
        {
            return $"{Severity}: {(string.IsNullOrEmpty(MethodName) ? "" : $"[{MethodName}]")} {Message}";
        }
    }

    public partial class Client
    {
        /// <summary> Diagnostics bag for this client. </summary>
        private List<Diagnostic> _diagnostics = new();
        /// <summary> Provides diagnostic summary of actions taken by the client. </summary>
        public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;
        private readonly LogLevel _logLevel = LogLevel.None;


        private void LogError(string message) => LogError(null, message);
        private void LogError(string? methodName, string message)
        {
            if (this._logLevel != LogLevel.None) _diagnostics.Add(new Diagnostic(methodName, message, DiagnosticSeverity.Error));
        }
        private void LogWarning(string message) => LogWarning(null, message);
        private void LogWarning(string? methodName, string message)
        {
            if (this._logLevel >= LogLevel.ErrorsAndWarnings) _diagnostics.Add(new Diagnostic(methodName, message, DiagnosticSeverity.Warning));
        }
        private void LogMessage(string message) => LogMessage(null, message);
        private void LogMessage(string? methodName, string message)
        {
            if (this._logLevel == LogLevel.Verbose) _diagnostics.Add(new Diagnostic(methodName, message, DiagnosticSeverity.Message));
        }
    }
}
