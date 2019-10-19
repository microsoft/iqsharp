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



    public class MockShell : IShellServer
    {
        public event Action<Message> KernelInfoRequest;
        public event Action<Message> ExecuteRequest;
        public event Action<Message> ShutdownRequest;

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

    public class MockChannel : IChannel
    {
        public List<string> errors = new List<string>();
        public List<string> msgs = new List<string>();

        public void Display(object displayable)
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
