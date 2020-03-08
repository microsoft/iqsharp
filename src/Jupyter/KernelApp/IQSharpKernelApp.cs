// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Jupyter.Core;
using System;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    /// A KernelApplication that trigger events in the Event Service 
    /// </summary>
    public class IQSharpKernelApp : KernelApplication
    {
        /// <summary>
        ///     Constructs a new application given properties describing a particular kernel,
        ///     and an action to configure services.
        /// </summary>
        /// <param name="properties">
        ///     Properties describing this kernel to clients.
        /// </param>
        /// <param name="configure">
        ///     An action to configure services for the new kernel application. This action is
        ///     called after all other kernel services have been configured, and is typically
        ///     used to provide an implementation of Microsoft.Jupyter.Core.IExecutionEngine
        ///     along with any services required by that engine.
        ///</param>
        public IQSharpKernelApp(KernelProperties properties, Action<ServiceCollection> configure) 
            : base(properties, configure)
        {
            KernelStarted += OnKernelStarted;
            KernelStopped += OnKernelStopped;
        }

        private void OnKernelStopped()
        {
            var eventService = this.GetService<IEventService>();
            eventService?.Trigger<KernelStoppedEvent, IQSharpKernelApp>(this);
        }

        private void OnKernelStarted(ServiceProvider serviceProvider)
        {
            var eventService = serviceProvider.GetService<IEventService>();
            eventService?.Trigger<KernelStartedEvent, IQSharpKernelApp>(this);
        }
    }

    /// <summary>
    ///     Event type for when the Kernel is started
    /// </summary>
    public class KernelStartedEvent : Event<IQSharpKernelApp>
    {
    }

    /// <summary>
    ///     Event type for when the Kernel is stopped
    /// </summary>
    public class KernelStoppedEvent : Event<IQSharpKernelApp>
    {
    }

    /// <summary>
    ///     Extension methods to make it easy to consume and trigger Kernel events
    /// </summary>
    public static class KernelEventsExtensions
    {
        /// <summary>
        ///     Gets the typed EventPubSub for the KernelStarted event
        /// </summary>
        /// <param name="eventService">The event service where the EventSubPub lives</param>
        /// <returns>The typed EventPubSub for the KernelStarted event</returns>
        public static EventPubSub<KernelStartedEvent, IQSharpKernelApp> OnKernelStarted(this IEventService eventService)
        {
            return eventService?.Events<KernelStartedEvent, IQSharpKernelApp>();
        }

        /// <summary>
        ///     Gets the typed EventPubSub for the KernelStopped event
        /// </summary>
        /// <param name="eventService">The event service where the EventSubPub lives</param>
        /// <returns>The typed EventPubSub for the KernelStopped event</returns>
        public static EventPubSub<KernelStoppedEvent, IQSharpKernelApp> OnKernelStopped(this IEventService eventService)
        {
            return eventService?.Events<KernelStoppedEvent, IQSharpKernelApp>();
        }
    }
}
