// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core.Protocol;

namespace Microsoft.Quantum.IQSharp.Jupyter
{

    public class CustomShellRouter : ICustomShellRouter
    {
        private readonly IDictionary<string, Action<Message>> shellHandlers = new Dictionary<string, Action<Message>>();
        private readonly ILogger<CustomShellRouter> logger;
        private IServiceProvider services;

        public CustomShellRouter(
            IServiceProvider services,
            ILogger<CustomShellRouter> logger
        )
        {
            this.logger = logger;
            this.services = services;
        }

        public void Handle(Message message)
        {
            if (shellHandlers.TryGetValue(message.Header.MessageType, out var handler))
            {
                handler.Invoke(message);
            }
            else
            {
                logger.LogWarning("Unrecognized custom shell message of type {Type}: {Message}", message.Header.MessageType, message);
            }
        }

        public void RegisterHandler(string messageType, Action<Message> handler)
        {
            shellHandlers[messageType] = handler;
        }

        public void RegisterHandlers<TAssembly>()
        {
            var handlers = typeof(TAssembly)
                .Assembly
                .GetTypes()
                .Where(t =>
                {
                    if (!t.IsClass && t.IsAbstract) { return false; }
                    var matched = t
                        .GetInterfaces()
                        .Contains(typeof(ICustomShellHandler));
                    this.logger.LogDebug("Class {Class} subclass of CustomShellHandler? {Matched}", t.FullName, matched);
                    return matched;
                })
                .Select(handlerType =>
                    ActivatorUtilities.CreateInstance(services, handlerType)
                )
                .Cast<ICustomShellHandler>();

            foreach (var handler in handlers)
            {
                logger.LogInformation("Registering handler for type: {Type}", handler.MessageType);
                ((ICustomShellRouter) this).RegisterHandler(handler);
            }
        }
    }
}
