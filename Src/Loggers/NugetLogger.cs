// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Quantum.IQSharp.Common
{
    /// <summary>
    /// A simple class to keep track of Nuget logs.
    /// </summary>
    public class NuGetLogger : NuGet.Common.LoggerBase
    {
        private ILogger _logger { get; set; }
        public List<NuGet.Common.ILogMessage> Logs { get; private set; }

        public NuGetLogger(ILogger logger)
        {
            _logger = logger;
            this.Logs = new List<NuGet.Common.ILogMessage>();
        }

        public static LogLevel MapLevel(NuGet.Common.LogLevel original)
        {
            switch (original)
            {
                case NuGet.Common.LogLevel.Error:
                    return LogLevel.Error;
                case NuGet.Common.LogLevel.Warning:
                    return LogLevel.Warning;
                case NuGet.Common.LogLevel.Information:
                    return LogLevel.Information;
                case NuGet.Common.LogLevel.Debug:
                    return LogLevel.Debug;
                default:
                    return LogLevel.Trace;
            }
        }

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
