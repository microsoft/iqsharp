using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Web.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.IQSharp
{
    [TestClass]
    public class HttpServerIntegrationTests
    {
        private string autoLoadPackagesEnvVarName = "IQSHARP_AUTO_LOAD_PACKAGES";
        private string originalAutoLoadPackages = string.Empty;
        
        [TestInitialize]
        public void SetTestEnvironment()
        {
            // Avoid loading default packages during this test by setting IQSHARP_AUTO_LOAD_PACKAGES to $null.
            // Microsoft.Quantum.Standard and related packages are built from the QuantumLibraries repo,
            // which is downstream of this one. So loading those packages could cause problems in E2E builds.
            originalAutoLoadPackages = Environment.GetEnvironmentVariable(autoLoadPackagesEnvVarName) ?? string.Empty;
            Environment.SetEnvironmentVariable(autoLoadPackagesEnvVarName, "$null");
        }

        [TestCleanup]
        public void RestoreEnvironment()
        {
            Environment.SetEnvironmentVariable(autoLoadPackagesEnvVarName, originalAutoLoadPackages);
        }

        [TestMethod]
        public async Task CompileAndSimulateViaApi()
        {
            string[] args = new string[]{ "server" };

            // Set the same configuration that Program.cs is using.
            Program.Configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json")
                .AddCommandLine(
                    args,
                    new Dictionary<string, string>()
                    {
                        ["--user-agent"] = "IQSHARP_USER_AGENT",
                        ["--hosting-env"] = "IQSHARP_HOSTING_ENV"
                    }
                )
                .Add(new NormalizedEnvironmentVariableConfigurationSource
                {
                    Prefix = "IQSHARP_",
                    Aliases = new Dictionary<string, string>
                    {
                        ["USER_AGENT"] = "UserAgent",
                        ["HOSTING_ENV"] = "HostingEnvironment",
                        ["LOG_PATH"] = "LogPath",
                        ["AUTO_LOAD_PACKAGES"] = "AutoLoadPackages",
                        ["AUTO_OPEN_NAMESPACES"] = "AutoOpenNamespaces",
                        ["SKIP_AUTO_LOAD_PROJECT"] = "SkipAutoLoadProject",
                    }
                })
                .Build();

            using (IWebHost server = Program.GetHttpServer(args))
            {
                server.Start();

                // TODO: Use constant strings for these http requests. Port and paths.

                // Compile a snippet with the server and get back a list of now executable operations.
                var compileResult = await "http://localhost:8888/api"
                    .AppendPathSegment("Snippets")
                    .AppendPathSegment("compile")
                    .PostJsonAsync(new CompileSnippetModel
                     {
                         Code = SNIPPETS.HelloQ
                     })
                    .ReceiveJson<Response<string[]>>();

                Assert.AreEqual(Status.Success, compileResult.Status);
                Assert.AreEqual("HelloQ", compileResult.Result.First());

                // Now simulate the operation and check the output is as expected.
                var simulateResult = await "http://localhost:8888/api"
                    .AppendPathSegment("Snippets")
                    .AppendPathSegment("HelloQ")
                    .AppendPathSegment("simulate")
                    .GetAsync()
                    .ReceiveJson<Response<object>>();

                Assert.AreEqual(Status.Success, simulateResult.Status);
                Assert.AreEqual("Hello from quantum world!", simulateResult.Messages.First());
            }
        }
    }
}
