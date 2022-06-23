// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.IQSharp.Common;

/// <summary>
///      Abstracts error and warning messages for conditions that may commonly
///      occur during IQ# sessions.
/// </summary>
public static class CommonMessages
{
    public record UserMessage(
        string Text,
        string? Hint = null
    );

    public record NoSuchOperation(string OperationName) : UserMessage(
        Text: $"No Q# operation with name `{OperationName}` has been defined.",
        Hint: $"You may have misspelled the name `{OperationName}`, or you may have forgotten to run a cell above."
    );
}
