// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// List of arguments that are part of the PackageLoaded event.
    /// </summary>
    public class PackageLoadedEventArgs : EventArgs
    {
        public PackageLoadedEventArgs(string id, string version, TimeSpan duration)
        {
            this.PackageId = id;
            this.PackageVersion= version;
            this.Duration = duration;
        }

        /// <summary>
        /// The nuget Package id.
        /// </summary>
        public string PackageId { get; }

        /// <summary>
        /// The nuget Package version.
        /// </summary>
        public string PackageVersion { get; }

        /// <summary>
        /// The total time the load package operation took.
        /// </summary>
        public TimeSpan Duration { get; }
    }


    /// <summary>
    /// This service keeps track of references (assemblies) needed for compilation
    /// and exposes them in the different formats, depending on what each compiler need.
    /// </summary>
    public interface IReferences
    {
        /// <summary>
        /// This event is triggered when a package is successfully loaded.
        /// </summary>
        event EventHandler<PackageLoadedEventArgs> PackageLoaded;

        /// <summary>
        /// The plain list of Assemblies to be used as References. 
        /// </summary>
        ImmutableArray<AssemblyInfo> Assemblies { get; }

        /// <summary>
        /// The compiler metadata information for the currently loaded assemblies.
        /// </summary>
        CompilerMetadata CompilerMetadata { get; }

        /// <summary>
        /// The list of Nuget Packages that are installed for compilation and execution.
        /// </summary>
        IEnumerable<string>? Packages { get; }

        /// <summary>
        /// Adds a nuget package and its corresponding assemblies to the list of references.
        /// </summary>
        Task AddPackage(string name, Action<string>? statusCallback = null);
    }
}
