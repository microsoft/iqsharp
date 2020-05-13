using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Jupyter;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
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
        public AzureClientMagicBase(IAzureClient azureClient, string keyword, Documentation docs):
            base(keyword, docs)
        {
            this.AzureClient = azureClient;
        }

        /// <inheritdoc/>
        public override ExecutionResult Run(string input, IChannel channel) =>
            RunAsync(input, channel).GetAwaiter().GetResult();

        /// <summary>
        ///     Executes the magic command functionality for the given input.
        /// </summary>
        public abstract Task<ExecutionResult> RunAsync(string input, IChannel channel);
    }
}
