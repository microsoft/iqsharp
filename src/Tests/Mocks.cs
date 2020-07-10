// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Microsoft.Quantum.IQSharp;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Tests.IQSharp
{
    public class MockKernelOptions : IOptions<KernelContext>
    {
        public KernelContext Value => new KernelContext();
    }

    public class MockNugetOptions : IOptions<NugetPackages.Settings>
    {
        public MockNugetOptions(string[] versions)
        {
            this.Value = new NugetPackages.Settings()
            {
                DefaultPackageVersions = versions
            };
        }

        public NugetPackages.Settings Value { get; }
    }



    public class MockShell : IShellServer
    {
        public event Action<Message>? KernelInfoRequest;
        public event Action<Message>? ExecuteRequest;
        public event Action<Message>? ShutdownRequest; 

        internal void Handle(Message message)
        {
            (message.Header.MessageType switch {
                "kernel_info_request" => KernelInfoRequest,
                "execute_request" => ExecuteRequest,
                "shutdown_request" => ShutdownRequest,
                _ => null
            })?.Invoke(message);
        }

        public void SendIoPubMessage(Message message)
        {
            throw new NotImplementedException();
        }

        public void SendShellMessage(Message message)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }
    }

    public class MockShellRouter : IShellRouter
    {
        private MockShell shell;
        public MockShellRouter(MockShell shell)
        {
            this.shell = shell;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task Handle(Message message)
        {
            shell.Handle(message);
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        public void RegisterFallback(Func<Message, Task?> fallback)
        {
            throw new NotImplementedException();
        }

        public void RegisterHandler(string messageType, Func<Message, Task?> handler)
        {
        }

        public void RegisterHandlers<TAssembly>()
        {
        }
    }

    public class MockUpdatableDisplay : IUpdatableDisplay
    {
        public void Update(object displayable)
        {
        }
    }

    public class MockChannel : IChannel
    {
        public List<string> errors = new List<string>();
        public List<string> msgs = new List<string>();

        public void Display(object displayable)
        {
            
        }

        public IUpdatableDisplay DisplayUpdatable(object displayable)
        {
            return new MockUpdatableDisplay();
        }

        public void Stderr(string message) => errors.Add(message);

        public void Stdout(string message) => msgs.Add(message);
    }

    public class MockOperationResolver : IOperationResolver
    {
        public OperationInfo Resolve(string input)
        {
            return new OperationInfo(null, null);
        }
    }
}
