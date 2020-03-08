// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// A event to track when services get initialized.
    /// This is useful for scenarios where we want to do something immediatly
    /// after a specific service is initialized, for example subscribing to the 
    /// service events for telemetry purposes.
    /// </summary>
    /// <typeparam name="TService">The type (usually the interface) of the service</typeparam>
    public class ServiceInitializedEvent<TService> : Event<TService>
    {
    }

    /// <summary>
    /// Extension methods to make it easy to consume and trigger ServiceInitialized events
    /// </summary>
    public static class ServiceInitializedEventExtensions
    {
        /// <summary>
        /// Instantiate and trigger the ServiceInitialized event, invoking all subscriber Actions
        /// </summary>
        /// <typeparam name="TService">The type (usually the interface) of the service</typeparam>
        /// <param name="eventService">The event service where the EventSubPub lives</param>
        /// <param name="service">The service that got initialized</param>
        public static void TriggerServiceInitialized<TService>(this IEventService eventService, TService service)
        {
            eventService?.Trigger<ServiceInitializedEvent<TService>, TService>(service);
        }

        /// <summary>
        /// Gets the typed EventPubSub for the ServiceInitialized event
        /// </summary>
        /// <typeparam name="TService">The type (usually the interface) of the service</typeparam>
        /// <param name="eventService">The event service where the EventSubPub lives</param>
        /// <returns>The typed EventPubSub for the ServiceInitialized event</returns>
        public static EventPubSub<ServiceInitializedEvent<TService>,TService> OnServiceInitialized<TService>(this IEventService eventService)
        {
            return eventService?.Events<ServiceInitializedEvent<TService>,TService>();
        }
    }
}