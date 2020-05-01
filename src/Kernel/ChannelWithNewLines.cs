// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Jupyter.Core;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    /// This is a Jupyter Core IChannel that wraps an existing IChannel and
    /// adds NewLine symbols (\\n)
    /// to every message that gets logged to Stdout and Stderror.
    /// </summary>
    public class ChannelWithNewLines : IChannel
    {
        /// <summary>
        ///     The existing channel that this channel wraps with new lines.
        /// </summary>
        public IChannel BaseChannel { get; }

        /// <summary>
        ///     Constructs a new channel, given a base channel to be wrapped
        ///     with newlines.
        /// </summary>
        public ChannelWithNewLines(IChannel original)
        {
            BaseChannel = original;
        }

        /// <summary>
        ///     Formats a given message for display to stdout or stderr.
        /// </summary>
        /// <param name="msg">The message to be formatted.</param>
        /// <returns>
        ///     <paramref name="msg" />, formatted with a trailing newline
        ///     (<c>\n</c>).
        /// </returns>
        public static string Format(string msg) => $"{msg}\n";

        /// <summary>
        ///     Writes a given message to the base channel's standard output,
        ///     but with a trailing newline appended.
        /// </summary>
        /// <param name="message">The message to be written.</param>
        public void Stdout(string message) => BaseChannel?.Stdout(Format(message));

        /// <summary>
        ///     Writes a given message to the base channel's standard error,
        ///     but with a trailing newline appended.
        /// </summary>
        /// <param name="message">The message to be written.</param>
        public void Stderr(string message) => BaseChannel?.Stderr(Format(message));

        /// <summary>
        ///     Displays a given object using the base channel.
        /// </summary>
        /// <param name="displayable">The object to be displayed.</param>
        /// <remarks>
        ///     Note that no newline is appended by this method, as the
        ///     displayable object need not be a string.
        /// </remarks>
        public void Display(object displayable) => BaseChannel?.Display(displayable);

        /// <summary>
        ///     Displays a given object using the base channel, allowing for
        ///     future updates.
        /// </summary>
        /// <param name="displayable">The object to be displayed.</param>
        /// <remarks>
        ///     Note that no newline is appended by this method, as the
        ///     displayable object need not be a string.
        /// </remarks>
        /// <returns>
        ///     An object that can be used to update the display in the future.
        /// </returns>
        public IUpdatableDisplay DisplayUpdatable(object displayable) => BaseChannel?.DisplayUpdatable(displayable);
    }
}
