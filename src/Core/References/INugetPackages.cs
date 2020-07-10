// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using NuGet.Packaging.Core;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// This service provides the ability to download and keep track of Nuget Packages.
    /// </summary>
    public interface INugetPackages
    {
        /// <summary>
        /// List of Packages already installed.
        /// </summary>
        IEnumerable<PackageIdentity> Items { get; }

        /// <summary>
        /// List of Assemblies from current Packages.
        /// </summary>
        IEnumerable<AssemblyInfo> Assemblies { get; }

        /// <summary>
        /// Add a package.
        /// </summary>
        Task<PackageIdentity> Add(string package, Action<string>? statusCallback = null);
    }
}
