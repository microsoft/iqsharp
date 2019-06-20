// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Jupyter.Core;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    /// This is a Jupyter Core IChannel that wraps an existing IChannel and 
    /// adds NewLine symbols (\\n)
    /// to every message that gets logged to Stdout and Stderror.
    /// </summary>
    public class ChannelWithNewLines : IChannel
    {
        public IChannel BaseChannel { get; }

        public ChannelWithNewLines(IChannel original)
        {
            BaseChannel = original;
        }

        public static string Format(string msg) => $"{msg}\n";

        public void Stdout(string message) => BaseChannel?.Stdout(Format(message));

        public void Stderr(string message) => BaseChannel?.Stderr(Format(message));

        public void Display(object displayable) => BaseChannel?.Display(displayable);
    }
}
