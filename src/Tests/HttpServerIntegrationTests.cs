using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Web.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.IQSharp
{
    [TestClass]
    public class HttpServerIntegrationTests
    {
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
