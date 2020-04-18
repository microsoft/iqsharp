﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
        public NugetPackages Init()
        {
            var service = Startup.Create<References>("Workspace");
            return service.Nugets;
        }

        [TestMethod]
        public async Task GetLatestVersion()
        {
            var mgr = Init();

            async Task TestOne(string pkg, string version)
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

            var mgr = new NugetPackages(new MockNugetOptions(versions), null);

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
            var pkgId = new PackageIdentity("Microsoft.Quantum.Chemistry", NuGetVersion.Parse("0.4.1901.3104"));

            using (var context = new SourceCacheContext())
            {
                var dependencies = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
                await mgr.FindDependencies(pkgId, context, dependencies);
                Assert.AreEqual(198, dependencies.Count());
            }
        }

        [TestMethod]
        public async Task ResolveDependencyGraph()
        {
            var mgr = Init();
            var pkgId = new PackageIdentity("Microsoft.Quantum.Chemistry", NuGetVersion.Parse("0.4.1901.3104"));
            
            using (var context = new SourceCacheContext())
            {
                var dependencies = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
                await mgr.FindDependencies(pkgId, context, dependencies);
                var list = mgr.ResolveDependencyGraph(pkgId, dependencies).ToArray();

                Assert.AreEqual(198, dependencies.Count());
                Assert.AreEqual(131, list.Length);
            }
        }


        [TestMethod]
        public async Task GetPackageDependencies()
        {
            var mgr = Init();
            var pkgId = new PackageIdentity("Microsoft.Quantum.Chemistry", NuGetVersion.Parse("0.4.1901.3104"));

            using (var context = new SourceCacheContext())
            {
                var dependencies = await mgr.GetPackageDependencies(pkgId, context);

                Assert.AreEqual(131, dependencies.Count());
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
            // Do this on an old version, to make sure we don't try to delete a loaded assembly:
            var version = NuGetVersion.Parse("0.3.1811.2802-preview ");
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
            var pkgId = new PackageIdentity("Microsoft.Quantum.Chemistry", NuGetVersion.Parse("0.4.1901.3104"));

            using (var context = new SourceCacheContext())
            {
                await mgr.Add(pkgId);

                var libs = mgr.GetAssemblies(pkgId).Select(s => s.Assembly.GetName().Name).ToArray();
                Assert.AreEqual(2, libs.Length);
                CollectionAssert.Contains(libs, "Microsoft.Quantum.Chemistry.DataModel");
                CollectionAssert.Contains(libs, "Microsoft.Quantum.Chemistry.Runtime");
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
                await mgr.Add("Microsoft.Quantum.Research::0.11.2003.3107");
                var libsResearch = mgr.Assemblies.Select(s => s.Assembly.GetName().Name).ToArray();
                Assert.AreEqual(start + 1, mgr.Items.Count());
                CollectionAssert.Contains(libsResearch, "Microsoft.Quantum.Research");
                CollectionAssert.Contains(libsResearch, "Microsoft.Quantum.Chemistry.DataModel");
                CollectionAssert.Contains(libsResearch, "Microsoft.Quantum.Chemistry.Runtime");

                await mgr.Add("Microsoft.Quantum.Chemistry::0.11.2003.3107");
                var libsChem = mgr.Assemblies.Select(s => s.Assembly.GetName().Name).ToArray();
                Assert.AreEqual(start + 2, mgr.Items.Count());
                // Chemistry assembly was already by research, no new Assemblies should be added:
                Assert.AreEqual(libsResearch.Length, libsChem.Length);

                // Make sure we're case insensitive.
                await mgr.Add("microsoft.quantum.chemistry::0.11.2003.3107  ");
                Assert.AreEqual(start + 2, mgr.Items.Count());
            }
        }
    }
}
