// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Jupyter.Core;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    /// A event to trigger when a client requests interruption of the current cell execution.
    /// </summary>
    public class KernelInterruptRequestedEvent : Event<IExecutionEngine>
    {
    }

    /// <summary>
    /// Extension methods to make it easy to consume and trigger KernelInterruptRequested events.
    /// </summary>
    public static class KernelInterruptRequestedEventExtensions
    {
        /// <summary>
        /// Instantiate and trigger the <see cref="KernelInterruptRequestedEvent"/> event, invoking all subscriber actions.
        /// </summary>
        /// <param name="eventService">The event service where the EventSubPub lives.</param>
        /// <param name="engine">The <see cref="IExecutionEngine"/> instance for which interrupt is requested.</param>
        public static void TriggerKernelInterruptRequested(this IEventService eventService, IExecutionEngine engine)
        {
            eventService?.Trigger<KernelInterruptRequestedEvent, IExecutionEngine>(engine);
        }

        /// <summary>
        /// Gets the typed EventPubSub for the InterruptRequested event.
        /// </summary>
        /// <param name="eventService">The event service where the EventSubPub lives.</param>
        /// <returns>The typed EventPubSub for the KernelInterruptRequested event.</returns>
        public static EventPubSub<KernelInterruptRequestedEvent, IExecutionEngine> OnKernelInterruptRequested(this IEventService eventService)
        {
            return eventService?.Events<KernelInterruptRequestedEvent, IExecutionEngine>();
        }
    }
}