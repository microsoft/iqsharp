// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

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
        /// Keeps track of what package version to use for certain packages specified in appsettings.json.
        /// This way we can better control what the correct version of Microsoft.Quantum
        /// packages to use, since all of them should ideally be in-sync.
        /// </summary>
        IReadOnlyDictionary<string, NuGetVersion> DefaultVersions { get; }

        SourceRepository GlobalPackagesSource { get; }

        /// <summary>
        /// Add a package.
        /// </summary>
        Task<PackageIdentity> Add(string package, Action<string>? statusCallback = null);

        Task<IEnumerable<SourcePackageDependencyInfo>> Get(PackageIdentity pkgId, Action<string>? statusCallback = null);
    }
}
