// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// <summary>
///      URIs of well-known resources used across the IQ# project.
/// </summary>
public static class KnownUris
{
    /// <summary>
    ///     The URI for the reference documentation for IQ# magic commands.
    /// </summary>
    public const string MagicCommandReference = "https://docs.microsoft.com/qsharp/api/iqsharp-magic/";

    public static string ReferenceForMagicCommand(string commandName) =>
        MagicCommandReference + commandName;
}
