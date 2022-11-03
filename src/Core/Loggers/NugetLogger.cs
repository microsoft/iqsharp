// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable

using Microsoft.Extensions.Logging;

namespace Microsoft.Quantum.IQSharp.Common
{
    /// <summary>
    /// A simple class to keep track of Nuget logs.
    /// </summary>
    public class NuGetLogger : NuGet.Common.LoggerBase
    {
        private ILogger? _logger { get; set; }
        public List<NuGet.Common.ILogMessage> Logs { get; private set; }

        public NuGetLogger(ILogger? logger)
        {
            _logger = logger;
            this.Logs = new List<NuGet.Common.ILogMessage>();
        }

        public static LogLevel MapLevel(NuGet.Common.LogLevel original) =>
            original switch
            {
                NuGet.Common.LogLevel.Error => LogLevel.Error,
                NuGet.Common.LogLevel.Warning => LogLevel.Warning,
                NuGet.Common.LogLevel.Information => LogLevel.Information,
                NuGet.Common.LogLevel.Debug => LogLevel.Debug,
                _ => LogLevel.Trace
            };

        public override void Log(NuGet.Common.ILogMessage m)
        {
            _logger?.Log(MapLevel(m.Level), m.Message);
            Logs.Add(m);
        }

        public override Task LogAsync(NuGet.Common.ILogMessage m)
        {
            Log(m);
            return Task.CompletedTask;
        }
    }
}
