// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Jupyter.Core.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    ///      Extension methods to be used with various IQ# and Jupyter objects.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        ///     Adds services required for the IQ# kernel to a given service
        ///     collection.
        /// </summary>
        public static T AddIQSharpKernel<T>(this T services)
        where T: IServiceCollection
        {
            services.AddSingleton<ISymbolResolver, SymbolResolver>();
            services.AddSingleton<IMagicSymbolResolver, MagicSymbolResolver>();
            services.AddSingleton<IExecutionEngine, Kernel.IQSharpEngine>();
            services.AddSingleton<IConfigurationSource, ConfigurationSource>();
            services.AddSingleton<INoiseModelSource, NoiseModelSource>();
            services.AddSingleton<ClientInfoListener>();

            return services;
        }

        internal static void RenderExecutionPath(this ExecutionPathTracer.ExecutionPathTracer tracer,
            IChannel channel,
            string executionPathDivId,
            int renderDepth,
            TraceVisualizationStyle style)
        {
            // Retrieve the `ExecutionPath` traced out by the `ExecutionPathTracer`
            var executionPath = tracer.GetExecutionPath();

            // Convert executionPath to JToken for serialization
            var executionPathJToken = JToken.FromObject(executionPath,
                new JsonSerializer() { NullValueHandling = NullValueHandling.Ignore });

            // Send execution path to JavaScript via iopub for rendering
            channel.SendIoPubMessage(
                new Message
                {
                    Header = new MessageHeader
                    {
                        MessageType = "render_execution_path"
                    },
                    Content = new ExecutionPathVisualizerContent
                    (
                        executionPathJToken,
                        executionPathDivId,
                        renderDepth,
                        style
                    )
                }
            );
        }

        private readonly static Regex UserAgentVersionRegex = new Regex(@"\[(.*)\]");

        internal static Version? GetUserAgentVersion(this IMetadataController? metadataController)
        {
            var userAgent = metadataController?.UserAgent;
            if (string.IsNullOrWhiteSpace(userAgent))
                return null;

            var match = UserAgentVersionRegex.Match(userAgent);
            if (match == null || !match.Success)
                return null;

            if (!Version.TryParse(match.Groups[1].Value, out Version version))
                return null;

            // return null for development versions that start with 0.0
            if (version.Major == 0 && version.Minor == 0)
                return null;

            return version;
        }

        internal static int EditDistanceFrom(this string s1, string s2)
        {
            // Uses the approach at
            // https://github.com/dotnet/samples/blob/main/csharp/parallel/EditDistance/Program.cs.
            var dist = new int[s1.Length + 1, s2.Length + 1];
            for (int i = 0; i <= s1.Length; i++) dist[i, 0] = i;
            for (int j = 0; j <= s2.Length; j++) dist[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    dist[i, j] = (s1[i - 1] == s2[j - 1]) ?
                        dist[i - 1, j - 1] :
                        1 + System.Math.Min(dist[i - 1, j],
                            System.Math.Min(dist[i, j - 1],
                                            dist[i - 1, j - 1]));
                }
            }

            return dist[s1.Length, s2.Length];
        }

        internal static IServiceProvider AddBuiltInMagicSymbols(this IServiceProvider serviceProvider)
        {
            serviceProvider.GetRequiredService<IMagicSymbolResolver>()
                .AddKernelAssembly<IQSharpKernelApp>()
                .AddKernelAssembly<AzureClient.AzureClient>();
            return serviceProvider;
        }
    }

}
