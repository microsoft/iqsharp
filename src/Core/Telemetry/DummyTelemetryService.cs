// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// Dummy telemetry service that does not do anything.
    /// This is used when telemetry is opted-out.
    /// </summary>
    public class DummyTelemetryService: ITelemetryService
    {
        public DummyTelemetryService(
                ILogger<DummyTelemetryService> logger
            )
        {
            logger.LogInformation("--> IQ# Telemetry opted-out. No telemetry data will be generated.");
        }
    }
}
