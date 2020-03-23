// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// A DependencyInjection service for the Event Publisher-Subscriber
    /// </summary>
    public interface IEventService
    {
        /// <summary>
        /// Gets the corresponding typed EventPubSub for the given event type
        /// </summary>
        /// <typeparam name="TEvent">The type of the event</typeparam>
        /// <typeparam name="TArgs">The type of the arguments of the event</typeparam>
        /// <returns>The typed EventPubSub for the given event type</returns>
        EventPubSub<TEvent, TArgs> Events<TEvent, TArgs>()
            where TEvent : Event<TArgs>;

        /// <summary>
        /// Instantiate and trigger the event, invoking all subscriber Actions
        /// </summary>
        /// <typeparam name="TEvent">The type of the event</typeparam>
        /// <typeparam name="TArgs">The type of the arguments of the event</typeparam>
        /// <param name="args">The arguments of the event</param>
        void Trigger<TEvent, TArgs>(TArgs args)
            where TEvent : Event<TArgs>, new();
    }
}
