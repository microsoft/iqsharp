// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.AzureClient;
using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Logging;

using static Microsoft.VisualStudio.TestTools.UnitTesting.Logging.Logger;
using System.Collections.Concurrent;

namespace Tests.IQSharp
{

    public class UnitTestLoggerConfiguration
    {
        public int? EventIdFilter { get; set; } = null;
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
    }

    internal class ActionDisposer : IDisposable
    {
        private readonly Action OnDispose;
        public ActionDisposer(Action onDispose)
        {
            this.OnDispose = onDispose;
        }

        public void Dispose() =>
            OnDispose();
    }

    /// <summary>
    ///      Forwards ASP.NET Core logging information to the unit testing
    ///      harness.
    /// </summary>
    public class UnitTestLogger : ILogger
    {
        private UnitTestLoggerConfiguration Config;
        private string CategoryName;
        private string? CurrentScope = null;
        public UnitTestLogger(string categoryName, UnitTestLoggerConfiguration? config = default)
        {
            this.CategoryName = categoryName;
            this.Config = config ?? new UnitTestLoggerConfiguration();
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            var oldScope = CurrentScope;
            CurrentScope = state?.ToString();
            return new ActionDisposer(() => CurrentScope = oldScope);
        }

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel == Config.LogLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (Config.EventIdFilter == null || Config.EventIdFilter == eventId)
            {
                if (CurrentScope != null)
                {
                    LogMessage(
                        $"[{CurrentScope} | {eventId}: {logLevel} // {CategoryName}] {formatter(state, exception)}"
                    );
                }
                else
                {
                    LogMessage(
                        $"[{eventId}: {logLevel} // {CategoryName}] {formatter(state, exception)}"
                    );
                }
            }
        }

    }

    public class UnitTestLogger<T> : UnitTestLogger, ILogger<T>
    {
        public UnitTestLogger(string? categoryName = null, UnitTestLoggerConfiguration? config = null)
        : base(categoryName ?? typeof(T).Name, config)
        {
        }
    }

    public sealed class UnitTestLoggerProvider : ILoggerProvider
    {
        private readonly UnitTestLoggerConfiguration Config;
        private readonly ConcurrentDictionary<string, UnitTestLogger> Loggers =
            new ConcurrentDictionary<string, UnitTestLogger>();

        public UnitTestLoggerProvider(UnitTestLoggerConfiguration config)
        {
            this.Config = config;
        }

        public ILogger CreateLogger(string categoryName) =>
            Loggers.GetOrAdd(categoryName, name => new UnitTestLogger(name, Config));

        public void Dispose() => Loggers.Clear();
    }

}
