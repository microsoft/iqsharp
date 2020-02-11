// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Jupyter.Core.Protocol;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    public interface ICustomShellHandler
    {
        public string MessageType { get; }
        public void Handle(Message message);
    }

    public interface ICustomShellRouter
    {
        public void RegisterHandler(string messageType, Action<Message> handler);

        public void RegisterHandler(ICustomShellHandler handler)
        {
            RegisterHandler(handler.MessageType, handler.Handle);
        }

        public void Handle(Message message);

        public void RegisterHandlers<TAssembly>();
    }
}