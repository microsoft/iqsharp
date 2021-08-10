﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Jupyter;
// NB: The name `Documentation` can be ambiguous in this context,
//     since we rely both on Microsoft.Quantum.Documentation and on
//     the name from Jupyter Core.
using JupyterDocumentation = Microsoft.Jupyter.Core.Documentation;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal static class AzureClientMagicExtensions
    {
        public static string ToMarkdown(this AzureClientError error) =>
            $"`{error.ToString()}`: {error.ToDescription()}";
    }

    /// <summary>
    ///     Base class used for Azure Client magic commands.
    /// </summary>
    public abstract class AzureClientMagicBase : AbstractMagic
    {
        /// <summary>
        ///     The object used by this magic command to interact with Azure.
        /// </summary>
        public IAzureClient AzureClient { get; }

        /// <summary>
        ///     Constructs the Azure Client magic command with the specified keyword
        ///     and documentation.
        /// </summary>
        /// <param name="azureClient">The <see cref="IAzureClient"/> object used to interact with Azure.</param>
        /// <param name="keyword">The name used to invoke the magic command.</param>
        /// <param name="docs">Documentation describing the usage of this magic command.</param>
        /// <param name="logger">Logger to be used for reporting issues from this magic command.</param>
        public AzureClientMagicBase(IAzureClient azureClient, string keyword, JupyterDocumentation docs, ILogger logger):
            base(keyword, docs, logger)
        {
            this.AzureClient = azureClient;
        }

        /// <inheritdoc/>
        public override ExecutionResult Run(string input, IChannel channel) =>
            RunCancellable(input, channel, CancellationToken.None);

        /// <inheritdoc/>
        public override ExecutionResult RunCancellable(string input, IChannel channel, CancellationToken cancellationToken) =>
            RunAsync(input, channel, cancellationToken).GetAwaiter().GetResult();

        /// <summary>
        ///     Executes the magic command functionality for the given input.
        /// </summary>
        public abstract Task<ExecutionResult> RunAsync(string input, IChannel channel, CancellationToken cancellationToken);
    }
}
