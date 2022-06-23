// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

#nullable enable

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// A simpler wrapper to be able to use ChannelWriter as a TextWriter.
    /// </summary>
    public class ChannelWriter : System.IO.TextWriter
    {
        private IChannel? Channel { get; }

        /// <summary>
        /// The default constructor.
        /// </summary>
        /// <param name="channel">The channel to write output to.</param>
        public ChannelWriter(IChannel? channel)
        {
            Channel = channel;
        }

        /// <inheritdoc/>
        public override Encoding Encoding => Encoding.UTF8;

        /// <inheritdoc/>
        public override void Write(string message) =>
            Channel?.Stdout(message);
    }
}
