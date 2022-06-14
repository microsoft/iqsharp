// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using Microsoft.Build.Locator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.IQSharp;

/// <summary>
///     Encapsulates tasks that are needed to initialize the entire unit
///     testing assembly.
/// </summary>
public class AssemblyInitialize
{
    public static VisualStudioInstance? vsi { get; private set; } = null;

    [AssemblyInitialize]
    static void Initialize()
    {
        // NB: MSBuildLocator must be used as early as possible.
        //     In Tool.csproj, we handle that by running during service
        //     configuration, but that fails in tests as the startup can
        //     be called multiple times — thus, we need to call once in
        //     the static constructor.
        vsi = MSBuildLocator.RegisterDefaults();
    }
}
