// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// Null telemetry service that does not do anything (does not log or send any data).
    /// This is used when telemetry is opted-out.
    /// </summary>
    public class NullTelemetryService: ITelemetryService
    {
        public NullTelemetryService(
                ILogger<NullTelemetryService> logger
            )
        {
            logger.LogInformation("--> IQ# Telemetry opted-out. No telemetry data will be generated or sent.");
        }
    }
}
