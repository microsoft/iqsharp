// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.IQSharp;

/// <summary>
///      Represents information that the kernel has about the client connected
///      to IQ#.
/// </summary>
public record ClientInformation
{
    /// <summary>
    ///      A string passed by the client containing additional information
    ///      that should be appended to the user agent (e.g.: packaging
    ///      metadata).
    /// </summary>
    public string? UserAgentExtra { get; init; }

    /// <summary>
    ///      A string passed by the client representing the name of the client.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    ///     A string passed by the client representing the environment in which
    ///     the client is running (e.g.: continuous integration, a hosted
    ///     notebook service, etc.).
    /// </summary>
    public string? HostingEnvironment { get; init; }

    /// <summary>
    ///     A string that is set to turn off the telemetry
    /// </summary>
    public string? TelemetryOptOut { get; init; }

    /// <summary>
    ///     A boolean, based on the TelemetryOptOut string property
    /// </summary>
    public bool IsTelemetryOptOut => !string.IsNullOrEmpty(TelemetryOptOut);
}
