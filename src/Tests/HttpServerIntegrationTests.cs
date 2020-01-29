using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Web.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using Tests.IQSharp;

namespace Tests.IQsharp
{
    [TestClass]
    public class HttpServerIntegrationTests
    {
        [TestMethod]
        public async Task CompileAndSimulateViaApi()
        {
            string[] args = new string[]{ "server" };

            Program.Configuration = new ConfigurationBuilder()
                //.AddEnvironmentVariables()
                //.AddJsonFile("appsettings.json")
                //.AddCommandLine(
                //    args,
                //    // We provide explicit aliases for those command line
                //    // options that specify client information, matching
                //    // the placeholder options that we define below.
                //    new Dictionary<string, string>()
                //    {
                //        ["--user-agent"] = "IQSHARP_USER_AGENT",
                //        ["--hosting-env"] = "IQSHARP_HOSTING_ENV"
                //    }
                //)
                .Build();

            using (IWebHost server = Program.GetHttpServer(args))
            {
                server.Start();

                // TODO: Use constant strings for these http requests. Port and paths.
                try
                {
                    var compileResult = await "http://localhost:8888/api"
                        .AppendPathSegment("snippets")
                        .AppendPathSegment("compile")
                        .PostJsonAsync(new CompileSnippetModel
                        {
                            Code = SNIPPETS.HelloQ
                        })
                        .ReceiveJson<Response<string[]>>();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            }
        }
    }
}
