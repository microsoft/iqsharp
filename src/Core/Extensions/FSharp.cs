// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


namespace Microsoft.Quantum.IQSharp;

public static partial class Extensions
{

    public static T? AsObj<T>(this FSharp.Core.FSharpOption<T> option)
    where T: class =>
        FSharp.Core.FSharpOption<T>.get_IsSome(option)
        ? option.Value
        : (T?)null;
}
