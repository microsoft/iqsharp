// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.IQSharp.Jupyter;

/// <summary>
///     Configuration values for controlling how compilation errors are
///     reported to clients.
/// </summary>
public enum CompilationErrorStyle
{
    /// <summary>
    ///      Specifies that diagnostics should be formatted as basic strings,
    ///      similar to MSBuild output logs.
    /// </summary>
    Basic,

    /// <summary>
    ///      Specifies that diagnostics should be formatted with annotated
    ///      source and additional links to documentation where appropriate.
    /// </summary>
    /// <seealso cref="FancyError" />
    Fancy
}
