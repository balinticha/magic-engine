# Event System Documentation

## Overview

The Event System in MagicThing uses a **directed, immediate, component-based** approach. Unlike global message busses, events here are raised *on specific entities*.

Systems subscribe to events that occur on entities possessing specific components. This ensures type safety and reduces unnecessary event filtering logic in your game code.

## Architecture (For Engine Developers)

The core of the system is the `EventManager` class.

### Key Concepts

1.  **Directed Events**: `Raise<TEvent>(Entity target, TEvent data)`
    *   Events are not broadcast to the world. They are sent to a specific `Entity`.
    *   This is crucial for things like `CollisionEvent`, `DamageEvent`, or `InteractEvent` where the context is local to an object.

2.  **Component-Based Subscription**: `Subscribe<TComponent, TEvent>(...)`
    *   A system doesn't just listen for "DamageEvents". It listens for "DamageEvents on entities that have a `Health` component".
    *   This acts as an automatic filter. The callback is only invoked if the target entity has the `TComponent`.
    *   The callback receives the entity wrapped as an `Entity<TComponent>`, giving immediate access to the relevant state.

3.  **Immediate Execution**
    *   Events are not queued for the end of the frame.
    *   Calling `Raise` immediately iterates through subscriptions and invokes callbacks synchronously.
    *   *Note: This means you must be careful about modifying collections (like removing entities) inside event handlers.*

4.  **Generics & Boxing**
    *   The system uses `IEventSubscription` to store generic subscriptions in a list keyed by the Event Type.
    *   While this involves some boxing/unboxing, it provides a very clean API for the user.

## Usage Guide (For Engine Users)

### 1. Defining an Event

Events are just simple C# classes. They can hold data or be empty markers.

```csharp
public class DamageEvent
{
    public int Amount;
    public Entity Attacker;
}

public class DeathEvent { } // Empty event
```

### 2. Subscribing to Events

You typically subscribe in `OnSceneLoad()` (part of the `EntitySystem` lifecycle).

**Syntax:** `Events.Subscribe<ComponentType, EventType>(CallbackMethod);`

```csharp
public class HealthSystem : EntitySystem
{
    public override void OnSceneLoad()
    {
        // specific syntax: Listen for DamageEvent, but ONLY on entities with Health
        Events.Subscribe<Health, DamageEvent>(OnDamageReceived);
    }

    private void OnDamageReceived(Entity<Health> entity, DamageEvent ev)
    {
        // We automatically get the Health component via the generic wrapper
        ref var health = ref entity.Get();
        
        health.Current -= ev.Amount;
        
        if (health.Current <= 0)
        {
            // Raise another event!
            Events.Raise(entity, new DeathEvent());
        }
    }
}
```

### 3. Raising Events

To trigger an event, use `Events.Raise(targetEntity, eventObject)`.

```csharp
// Example: A sword hitting an enemy
public void OnSwordHit(Entity enemy, int damage)
{
    Events.Raise(enemy, new DamageEvent 
    { 
        Amount = damage,
        Attacker = _playerEntity 
    });
}
```

### Why this architecture?
If we are targetting entities already, why do we need to specify components for listeners?  
The reason is better decoupling. For example, a SwordHitbox shouldn't need to care about what entities it hits. It can just raise the event for everything and forgot about it. Similarly, 
a BossDamageable system shouldn't have to sort though every single SwordHit event to see if it's meant for it or if for the PlayerDamageable system. It just subscribes with the condition that
the entity has a BossDamageable component, and the even system handles everything else for you.

### Best Practices

*   **Define Payload Classes**: Prefer creating dedicated classes for events (e.g., `JumpEvent`) rather than passing primitives.
*   **Don't Overuse**: For simple logic that happens every frame (like movement), use `System.Update()` and iterate components. Use Events for *discrete* actions (collisions, triggers, UI interactions).
*   **Unsubscribing**: `EventManager` has an `Unsubscribe` method, but `SystemManager` currently does not automatically unsubscribe systems on unload. *Clarification needed: Check if your engine clears all events on scene change.*
