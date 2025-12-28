using System;
using System.Collections.Generic;
using System.Linq;
using DefaultEcs;
using MagicThing.Engine.Base.EntityWrappers;

namespace MagicThing.Engine.Base.Events;

/// <summary>
/// A non-generic interface for an event subscription. This allows us to store
/// subscriptions for different event and component types in the same list.
/// </summary>
internal interface IEventSubscription
{
    /// <summary>
    /// Tries to invoke the callback if the target entity matches the subscription's criteria
    /// (e.g., has the required component) and the event type is correct.
    /// </summary>
    /// <param name="entity">The entity the event was raised on.</param>
    /// <param name="eventData">The event data object.</param>
    void TryInvoke(Entity entity, object eventData);
}

/// <summary>
/// A strongly-typed implementation of IEventSubscription. It holds the actual
/// callback and performs the necessary type checks and component fetching.
/// </summary>
/// <typeparam name="TComponent">The component type the subscription listens for.</typeparam>
/// <typeparam name="TEvent">The event type the subscription listens for.</typeparam>
internal class EventSubscription<TComponent, TEvent> : IEventSubscription where TEvent : class
{
    private readonly Action<Entity<TComponent>, TEvent> _callback;
    public Action<Entity<TComponent>, TEvent> Callback => _callback;

    public EventSubscription(Action<Entity<TComponent>, TEvent> callback)
    {
        _callback = callback;
    }

    public void TryInvoke(Entity entity, object eventData)
    {
        if (entity.TryGet(out Entity<TComponent> componentEntity) && eventData is TEvent ev)
        {
            _callback(componentEntity, ev);
        }
    }
}


/// <summary>
/// Manages directed event subscriptions and raising.
/// Events are raised on a specific entity and will only trigger callbacks
/// for systems that subscribed with a component that the entity possesses.
/// </summary>
public class EventManager
{
    private readonly Dictionary<Type, List<IEventSubscription>> _subscriptions = new();

    /// <summary>
    /// Subscribes a callback to a specific event type, which will only be invoked
    /// if the event is raised on an entity that has the specified component.
    /// </summary>
    /// <typeparam name="TComponent">The component the entity must have.</typeparam>
    /// <typeparam name="TEvent">The event class to listen for.</typeparam>
    /// <param name="callback">The method to call, with signature (EntityTComponent>, TEvent) => {}.</param>
    public void Subscribe<TComponent, TEvent>(Action<Entity<TComponent>, TEvent> callback) where TEvent : class
    {
        var eventType = typeof(TEvent);
        if (!_subscriptions.TryGetValue(eventType, out var subscriptionList))
        {
            subscriptionList = new List<IEventSubscription>();
            _subscriptions[eventType] = subscriptionList;
        }

        var subscription = new EventSubscription<TComponent, TEvent>(callback);
        subscriptionList.Add(subscription);
    }

    public void Unsubscribe<TComponent, TEvent>(Action<Entity<TComponent>, TEvent> callback) where TEvent : class
    {
        var eventType = typeof(TEvent);
        if (!_subscriptions.TryGetValue(eventType, out var subscriptionList))
            return;

        // Find the subscription that matches the specific callback instance.
        // We need to check the type and then compare the delegate.
        var subscriptionToRemove = subscriptionList
            .OfType<EventSubscription<TComponent, TEvent>>()
            .FirstOrDefault(sub =>
                sub.Callback == callback);

        if (subscriptionToRemove != null)
        {
            subscriptionList.Remove(subscriptionToRemove);

            if (subscriptionList.Count == 0)
            {
                _subscriptions.Remove(eventType);
            }
        }
    }

    /// <summary>
    /// Raises a directed event on a specific entity.
    /// This is executed immediately and will trigger all matching subscribed callbacks.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event class.</typeparam>
    /// <param name="target">The entity to raise the event on.</param>
    /// <param name="ev">The event data class instance.</param>
    public TEvent Raise<TEvent>(Entity target, TEvent ev) where TEvent : class
    {
        if (!target.IsEnabled()) // Don't raise events on disabled/destroyed entities
            return ev;
            
        var eventType = typeof(TEvent);
        if (_subscriptions.TryGetValue(eventType, out var subscriptionList))
        {
            // We iterate through a copy in case a callback modifies the collection
            // although this is generally bad practice. For immediate events, it's safer.
            foreach (var subscription in subscriptionList.ToArray())
            {
                // The subscription itself handles the component check.
                subscription.TryInvoke(target, ev);
            }
        }

        return ev;
    }
}