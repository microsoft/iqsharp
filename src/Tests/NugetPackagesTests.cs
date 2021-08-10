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
        public static readonly NuGetVersion QDK_LIBRARIES_VERSION = NuGetVersion.Parse("0.12.20070124");

        public NugetPackages Init()
        {
            return Startup.Create<NugetPackages>("Workspace");
        }

        [TestMethod]
        public async Task GetLatestVersion()
        {
            var mgr = Init();

            async Task TestOne(string pkg, string? version)
            {
                var actual = await mgr.GetLatestVersion(pkg);

                if (version == null)
                {
                    Assert.IsNull(actual);
                }
                else
                {
                    var expected = NuGetVersion.Parse(version);
                    Assert.IsNotNull(actual);
                    Assert.IsTrue(actual >= expected);
                }
            }

            await TestOne("Microsoft.Quantum", null);
            await TestOne("Newtonsoft.Json", "12.0.1");
        }


        [TestMethod]
        public async Task GetDefaultVersion()
        {
            var version = "0.0.1101.3104-alpha";
            var nuVersion = NuGetVersion.Parse(version);

            var versions = new string[]
            {
                $"Microsoft.Quantum.Standard::{version}",
                $"Microsoft.Quantum.Quantum.Development.Kit::{version}",
                $"Microsoft.Quantum.Chemistry::{version}"
            };

            var mgr = new NugetPackages(new MockNugetOptions(versions), null, eventService: null);

            Assert.AreEqual(nuVersion, await mgr.GetLatestVersion("Microsoft.Quantum.Standard"));
            Assert.AreEqual(nuVersion, await mgr.GetLatestVersion("Microsoft.Quantum.Chemistry"));
            Assert.AreEqual(nuVersion, await mgr.GetLatestVersion("microsoft.quantum.chemistry"));
            Assert.AreEqual(nuVersion, await mgr.GetLatestVersion(" microsoft.quantum.chemistry  "));
            Assert.AreNotEqual(nuVersion, await mgr.GetLatestVersion("Microsoft.Quantum.Research"));
        }

        [TestMethod]
        public async Task FindDependencies()
        {
            var mgr = Init();
            var pkgId = new PackageIdentity("Microsoft.Quantum.Chemistry", QDK_LIBRARIES_VERSION);

            using (var context = new SourceCacheContext())
            {
                await mgr.FindDependencies(pkgId, context);
                Assert.AreEqual(144, mgr.AvailablePackages.Count());
            }
        }

        [TestMethod]
        public async Task ResolveDependencyGraph()
        {
            var mgr = Init();
            var pkgId = new PackageIdentity("Microsoft.Quantum.Chemistry", QDK_LIBRARIES_VERSION);
            
            using (var context = new SourceCacheContext())
            {
                await mgr.FindDependencies(pkgId, context);
                var list = mgr.ResolveDependencyGraph(pkgId).ToArray();

                Assert.AreEqual(144, mgr.AvailablePackages.Count());
                Assert.AreEqual(107, list.Length);
            }
        }


        [TestMethod]
        public async Task GetPackageDependencies()
        {
            var mgr = Init();
            var pkgId = new PackageIdentity("Microsoft.Quantum.Chemistry", QDK_LIBRARIES_VERSION);

            using (var context = new SourceCacheContext())
            {
                var dependencies = await mgr.GetPackageDependencies(pkgId, context);

                Assert.AreEqual(107, dependencies.Count());
            }
        }

        [TestMethod]
        public async Task DownloadPackages()
        {
            var mgr = Init();

            void ClearCache(string pkgName, NuGetVersion pkgVersion)
            {
                var pkg = new PackageIdentity(pkgName, pkgVersion);
                var localPkg = LocalFolderUtility.GetPackageV3(SettingsUtility.GetGlobalPackagesFolder(mgr.NugetSettings), pkg, mgr.Logger);

                if (localPkg != null)
                {
                    Directory.Delete(Path.GetDirectoryName(localPkg.Path), true);
                }
            }

            // Remove "Microsoft.Quantum.Chemistry" and "Microsoft.Quantum.Research" from local cache,
            // Do this on an old/different version, to make sure we don't try to delete an assembly already loaded by some other test:
            var version = NuGetVersion.Parse("0.5.1904.1302");
            ClearCache("Microsoft.Quantum.Chemistry", version);
            ClearCache("Microsoft.Quantum.Research", version);

            var researchPkg = new PackageIdentity("Microsoft.Quantum.Research", version);
            var chemPkg = new PackageIdentity("Microsoft.Quantum.Chemistry", version);

            using (var context = new SourceCacheContext())
            {
                var dependencies = await mgr.GetPackageDependencies(researchPkg, context);

                Assert.IsFalse(mgr.IsInstalled(researchPkg));
                Assert.IsFalse(mgr.IsInstalled(chemPkg));

                await mgr.DownloadPackages(context, dependencies);

                Assert.IsTrue(mgr.IsInstalled(researchPkg));
                Assert.IsTrue(mgr.IsInstalled(chemPkg));
            }
        }

        [TestMethod]
        public async Task GetAssemblies()
        {
            var mgr = Init();
            var pkgId = new PackageIdentity("Microsoft.Quantum.Chemistry.DataModel", QDK_LIBRARIES_VERSION);

            using (var context = new SourceCacheContext())
            {
                await mgr.Add(pkgId);

                var libs = mgr.GetAssemblies(pkgId).Select(s => s.Assembly.GetName().Name).ToArray();
                Assert.AreEqual(1, libs.Length);
                CollectionAssert.Contains(libs, "Microsoft.Quantum.Chemistry.DataModel");
            }
        }


        [TestMethod]
        public async Task AddPackage()
        {
            var mgr = Init();

            using (var context = new SourceCacheContext())
            {
                var start = mgr.Items.Count();

                // Note that since we depend on the internal structure of a
                // package for this test, and since that can change without
                // breaking user code, we use a known-good version instead of
                // the latest for the purpose of this test.
                await mgr.Add($"Microsoft.Quantum.Research::{QDK_LIBRARIES_VERSION}");
                var libsResearch = mgr.Assemblies.Select(s => s.Assembly.GetName().Name).ToArray();
                Assert.AreEqual(start + 1, mgr.Items.Count());
                CollectionAssert.Contains(libsResearch, "Microsoft.Quantum.Research.Simulation.Qsp");
                CollectionAssert.Contains(libsResearch, "Microsoft.Quantum.Chemistry.DataModel");
                CollectionAssert.Contains(libsResearch, "Microsoft.Quantum.Chemistry.Runtime");

                await mgr.Add($"Microsoft.Quantum.Chemistry::{QDK_LIBRARIES_VERSION}");
                var libsChem = mgr.Assemblies.Select(s => s.Assembly.GetName().Name).ToArray();
                Assert.AreEqual(start + 2, mgr.Items.Count());
                // Chemistry assembly was already by research, no new Assemblies should be added:
                Assert.AreEqual(libsResearch.Length, libsChem.Length);

                // Make sure we're case insensitive.
                await mgr.Add($"microsoft.quantum.chemistry::{QDK_LIBRARIES_VERSION}  ");
                Assert.AreEqual(start + 2, mgr.Items.Count());
            }
        }

        [TestMethod]
        public async Task AddInvalidPackage()
        {
            var mgr = Init();

            using (var context = new SourceCacheContext())
            {
                var start = mgr.Items.Count();

                await Assert.ThrowsExceptionAsync<NuGet.Resolver.NuGetResolverInputException>(() => mgr.Add("microsoft.invalid.quantum"));
                Assert.AreEqual(start, mgr.Items.Count());
            }
        }
    }
}
