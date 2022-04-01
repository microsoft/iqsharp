// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Quantum.IQSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Tests.IQSharp
{
    [TestClass]
    public class NugetPackagesTests
    {
        /// This is the version of the QDK libraries that we use to run tests
        /// that load packages and compile Q# code that depend on them.
        /// We use a known-good version to avoid breaking IQ# tests due to changes in Libraries
        /// also, to make sure an end-to-end QDK build does not have circular build dependencies
        /// between Libraries and IQ#.
        public static readonly NuGetVersion QDK_LIBRARIES_VERSION = NuGetVersion.Parse("0.24.201332");

        public NugetPackages Init()
        {
            return Startup.Create<NugetPackages>("Workspace");
        }

        [TestMethod]
        public async Task GetLatestVersion()
        {
            var mgr = Init();
        }
    }
}
