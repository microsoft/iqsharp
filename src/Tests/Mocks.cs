// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Microsoft.Quantum.IQSharp;

#if TELEMETRY
using Microsoft.Applications.Events;
#endif

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
        public event Action<Message> KernelInfoRequest;
        public event Action<Message> ExecuteRequest;
        public event Action<Message> ShutdownRequest; 

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

        public void Handle(Message message)
        {
            shell.Handle(message);
        }

        public void RegisterFallback(Action<Message> fallback)
        {
            throw new NotImplementedException();
        }

        public void RegisterHandler(string messageType, Action<Message> handler)
        {
            throw new NotImplementedException();
        }

        public void RegisterHandlers<TAssembly>()
        {
            throw new NotImplementedException();
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

        public IUpdatableDisplay DisplayUpdatable(object displayable)
        {
            throw new NotImplementedException();
        }


        public void Stderr(string message) => errors.Add(message);

        public void Stdout(string message) => msgs.Add(message);
    }

    #if TELEMETRY
    public class MockTelemetryLogger : ILogger
    {
        public List<EventProperties> Events = new List<EventProperties>();

        public EVTStatus LogEvent(EventProperties evt)
        {
            Events.Add(evt);
            return EVTStatus.OK;
        }

        public Task<SendResult> LogEventAsync(EventProperties evt)
        {
            Events.Add(evt);
            return Task.FromResult(new SendResult(ResultStatus.Send));
        }

        public EVTStatus SetContext(string name, string value, PiiKind piiKind = PiiKind.None) =>
            throw new NotImplementedException();

        public EVTStatus SetContext(string name, double value, PiiKind piiKind = PiiKind.None) =>
            throw new NotImplementedException();

        public EVTStatus SetContext(string name, long value, PiiKind piiKind = PiiKind.None) =>
            throw new NotImplementedException();

        public EVTStatus SetContext(string name, sbyte value, PiiKind piiKind = PiiKind.None) =>
            throw new NotImplementedException();

        public EVTStatus SetContext(string name, short value, PiiKind piiKind = PiiKind.None) =>
            throw new NotImplementedException();

        public EVTStatus SetContext(string name, int value, PiiKind piiKind = PiiKind.None) =>
            throw new NotImplementedException();

        public EVTStatus SetContext(string name, byte value, PiiKind piiKind = PiiKind.None) =>
            throw new NotImplementedException();

        public EVTStatus SetContext(string name, ushort value, PiiKind piiKind = PiiKind.None) =>
            throw new NotImplementedException();

        public EVTStatus SetContext(string name, uint value, PiiKind piiKind = PiiKind.None) =>
            throw new NotImplementedException();

        public EVTStatus SetContext(string name, bool value, PiiKind piiKind = PiiKind.None) =>
            throw new NotImplementedException();

        public EVTStatus SetContext(string name, DateTime value, PiiKind piiKind = PiiKind.None) =>
            throw new NotImplementedException();

        public EVTStatus SetContext(string name, Guid value, PiiKind piiKind = PiiKind.None) =>
            throw new NotImplementedException();
    }
    #endif

}
