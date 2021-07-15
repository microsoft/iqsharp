// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// A typed Event, similar to EventHandler<> but without actually being a delegate
    /// </summary>
    /// <typeparam name="TArgs">The type of the arguments</typeparam>
    public class Event<TArgs>
    {
        public TArgs Args { get; set; }
    }

    /// <summary>
    /// A basic multi-thread implementation of a publisher-subscriber based on
    /// the type of the event.
    /// </summary>
    public class EventPubSub
    {
        private readonly ILogger Logger;
        public EventPubSub(ILogger logger)
        {
            Logger = logger;
        }

        private readonly ConcurrentDictionary<Type, Collection<object>> _Subscribers
            = new ConcurrentDictionary<Type, Collection<object>>();

        /// <summary>
        /// Add a new subscriber action to the Subscriber list for the give event type.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event</typeparam>
        /// <typeparam name="TArgs">The type of the arguments of the event</typeparam>
        /// <param name="action">The action (of type Action<TArg>) to be executed when the event is triggered</param>
        public void Subscribe<TEvent, TArgs>(Action<TArgs> action)
            where TEvent : Event<TArgs>
        {
            var subscribers = _Subscribers.GetOrAdd(typeof(TEvent), (_) => new Collection<object>());
            lock (subscribers)
            {
                subscribers.Add(action);
            }
            Logger.LogInformation($"Event Subscription Added to '{typeof(TEvent).Name}'");
        }

        /// <summary>
        /// Remove a subscriber action from the Subscriber list for the give event type.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event</typeparam>
        /// <typeparam name="TArgs">The type of the arguments of the event</typeparam>
        /// <param name="action">The action (of type Action<TArg>) to be removed</param>
        public void Unsubscribe<TEvent, TArgs>(Action<TArgs> action)
            where TEvent : Event<TArgs>
        {
            if (_Subscribers.TryGetValue(typeof(TEvent), out var subscribers))
            {
                lock (subscribers)
                {
                    subscribers.Remove(action);
                }
                Logger.LogInformation($"Event Subscription Removed from '{typeof(TEvent).Name}'");
            }
        }

        /// <summary>
        /// Trigger the event, invoking all subscriber Actions
        /// </summary>
        /// <typeparam name="TEvent">The type of the event</typeparam>
        /// <typeparam name="TArgs">The type of the arguments of the event</typeparam>
        /// <param name="event">The event to be triggered</param>
        public void Trigger<TEvent, TArgs>(TEvent @event)
            where TEvent : Event<TArgs>
        {
            Logger.LogInformation($"Event Triggered for '{typeof(TEvent)}'");
            if (_Subscribers.TryGetValue(typeof(TEvent), out var subscribersList))
            {
                object[] subscribers;
                lock (subscribersList)
                {
                    subscribers = subscribersList.ToArray();
                }
                foreach (var subscriber in subscribers)
                {
                    var actionType = typeof(Action<>).MakeGenericType(typeof(TArgs));
                    var invokeMethod = actionType.GetMethod("Invoke", new Type[] {
                        typeof(TArgs)
                    });
                    invokeMethod.Invoke(subscriber, new object[] { @event.Args });
                    Logger.LogInformation($"Event Subscriber Invoked for '{typeof(TEvent).Name}'");
                }
            }
        }
    }

    /// <summary>
    /// A typed wrapper for the EventPubSub class.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event</typeparam>
    /// <typeparam name="TArgs">The type of the arguments of the event</typeparam>
    public class EventPubSub<TEvent, TArgs>
        where TEvent : Event<TArgs>
    {
        private readonly EventPubSub EventPubsub;

        public EventPubSub(EventPubSub eventPubSub)
        {
            EventPubsub = eventPubSub;
        }

        /// <summary>
        /// The event to subscribe
        /// </summary>
        public event Action<TArgs> On
        {
            add
            {
                EventPubsub.Subscribe<TEvent, TArgs>(value);
            }
            remove
            {
                EventPubsub.Unsubscribe<TEvent, TArgs>(value);
            }
        }

        /// <summary>
        /// Trigger the event, invoking all subscriber Actions
        /// </summary>
        /// <param name="event">The event to be triggered</param>
        public void Trigger(TEvent @event)
        {
            EventPubsub.Trigger<TEvent,TArgs>(@event);
        }
    }

    /// <summary>
    /// A DependencyInjection service for the Event Publisher-Subscriber
    /// </summary>
    public class EventService : IEventService
    {
        private readonly EventPubSub EventPubSub;
        public EventService(ILogger<EventService> logger)
        {
            EventPubSub = new EventPubSub(logger);
            this?.TriggerServiceInitialized<IEventService>(this);
        }

        /// <summary>
        /// Gets the corresponding typed EventPubSub for the given event type
        /// </summary>
        /// <typeparam name="TEvent">The type of the event</typeparam>
        /// <typeparam name="TArgs">The type of the arguments of the event</typeparam>
        /// <returns>The typed EventPubSub for the given event type</returns>
        public EventPubSub<TEvent, TArgs> Events<TEvent, TArgs>()
            where TEvent : Event<TArgs>
        {
            return new EventPubSub<TEvent, TArgs>(EventPubSub);
        }

        /// <summary>
        /// Instantiate and trigger the event, invoking all subscriber Actions
        /// </summary>
        /// <typeparam name="TEvent">The type of the event</typeparam>
        /// <typeparam name="TArgs">The type of the arguments of the event</typeparam>
        /// <param name="args">The arguments of the event</param>
        public void Trigger<TEvent,TArgs>(TArgs args)
            where TEvent:Event<TArgs>,new()
        {
            Events<TEvent, TArgs>().Trigger(new TEvent() { Args = args});
        }
    }
}
