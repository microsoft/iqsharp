// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Jupyter;
using System;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <inheritdoc />
    public class IQSharpKernelApp : KernelApplication
    {
        /// <inheritdoc />
        public IQSharpKernelApp(KernelProperties properties, Action<ServiceCollection> configure) 
            : base(properties, configure)
        {
            KernelStarted += OnKernelStarted;
            KernelStopped += OnKernelStopped;
        }

        /// <inheritdoc />
        public override ServiceProvider InitServiceProvider(IServiceCollection serviceCollection)
        {
            var serviceProvider = base.InitServiceProvider(serviceCollection);
            serviceProvider.GetRequiredService<ITelemetryService>();
            return serviceProvider;
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
        public static EventPubSub<KernelStartedEvent, IQSharpKernelApp>? OnKernelStarted(this IEventService eventService)
        {
            return eventService?.Events<KernelStartedEvent, IQSharpKernelApp>();
        }

        /// <summary>
        ///     Gets the typed EventPubSub for the KernelStopped event
        /// </summary>
        /// <param name="eventService">The event service where the EventSubPub lives</param>
        /// <returns>The typed EventPubSub for the KernelStopped event</returns>
        public static EventPubSub<KernelStoppedEvent, IQSharpKernelApp>? OnKernelStopped(this IEventService eventService)
        {
            return eventService?.Events<KernelStoppedEvent, IQSharpKernelApp>();
        }
    }
}
