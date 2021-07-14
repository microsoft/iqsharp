// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Microsoft.Quantum.Experimental;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
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
            services.AddSingleton<ISymbolResolver, Kernel.SymbolResolver>();
            services.AddSingleton<IMagicSymbolResolver, Kernel.MagicSymbolResolver>();
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
        
        internal static IEnumerable<QsDeclarationAttribute> GetAttributesByName(
            this OperationInfo operation, string attributeName,
            string namespaceName = "Microsoft.Quantum.Documentation"
        ) =>
            operation.Header.Attributes.Where(
                attribute =>
                    // Since QsNullable<UserDefinedType>.Item can be null,
                    // we use a pattern match here to make sure that we have
                    // an actual UDT to compare against.
                    attribute.TypeId.Item is UserDefinedType udt &&
                    udt.Namespace == namespaceName &&
                    udt.Name == attributeName
            );

        internal static bool TryAsStringLiteral(this TypedExpression expression, [NotNullWhen(true)] out string? value)
        {
            if (expression.Expression is QsExpressionKind<TypedExpression, Identifier, ResolvedType>.StringLiteral literal)
            {
                value = literal.Item1;
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        internal static IEnumerable<string> GetStringAttributes(
            this OperationInfo operation, string attributeName,
            string namespaceName = "Microsoft.Quantum.Documentation"
        ) => operation
            .GetAttributesByName(attributeName, namespaceName)
            .Select(
                attribute =>
                    attribute.Argument.TryAsStringLiteral(out var value)
                    ? value : null
            )
            .Where(value => value != null)
            // The Where above ensures that all elements are non-nullable,
            // but the C# compiler doesn't quite figure that out, so we
            // need to help it with a no-op that uses the null-forgiving
            // operator.
            .Select(value => value!);

        internal static IDictionary<string?, string?> GetDictionaryAttributes(
            this OperationInfo operation, string attributeName,
            string namespaceName = "Microsoft.Quantum.Documentation"
        ) => operation
            .GetAttributesByName(attributeName, namespaceName)
            .SelectMany(
                attribute => attribute.Argument.Expression switch
                {
                    QsExpressionKind<TypedExpression, Identifier, ResolvedType>.ValueTuple tuple =>
                        tuple.Item.Length != 2
                        ? throw new System.Exception("Expected attribute to be a tuple of two strings.")
                        : ImmutableList.Create((tuple.Item[0], tuple.Item[1])),
                    _ => ImmutableList<(TypedExpression, TypedExpression)>.Empty
                }
            )
            .ToDictionary(
                attribute => attribute.Item1.TryAsStringLiteral(out var value) ? value : null,
                attribute => attribute.Item2.TryAsStringLiteral(out var value) ? value : null
            );
    }
}
