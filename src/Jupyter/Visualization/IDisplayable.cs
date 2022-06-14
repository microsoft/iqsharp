// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Quantum.IQSharp.Jupyter;

public interface IDisplayable
{
    public bool TryAsDisplayData(string mimeType, [NotNullWhen(true)] out EncodedData? displayData);
}

public record DisplayableEncoder(string MimeType) : IResultEncoder
{
    public EncodedData? Encode(object obj)
    {
        if (obj is IDisplayable displayable && displayable.TryAsDisplayData(MimeType, out var encoded))
        {
            return encoded;
        }
        return null;
    }
}
