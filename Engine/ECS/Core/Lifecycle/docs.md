# Lifecycle System Documentation

## Overview

The Lifecycle System provides a safe way to destroy entities. Instead of deleting an entity immediately (which might
cause crashes if done while a system is iterating over it), this system allows marking entities for deletion, which are
then cleaned up safely at the end of the frame.

## Architecture (For Engine Developers)

### 1. DeferredEntityDisposalSystem

* **Bucket**: `ExecutionBucket.Cleanup` (Runs at the very end of the frame).
* **Mechanism**:
    1. Queries all entities with the `MarkedForDeath` component.
    2. Iterates through them.
    3. Calls `entity.Dispose()` on each.
    4. Logs the destroyed IDs (in verbose mode).

### 2. Components

* `MarkedForDeath`: An empty "Flag" component (marker). Presence indicates the entity should be destroyed this frame.

## Usage Guide (For Engine Users)

### 1. Killing an Entity Safely

Do not manage entity lifecycles yourself. The engine operates on a specific sequence of events being triggered when an entity  
is about to die and when it finally dies. There are operation helpers. Please refer to the EntitySystem documentation's
specific sections.

### 2. Intercepting and Preventing Entity Death

Sometimes you may want to intercept an entity's death before it happens (for example, to trigger a "second wind" mechanic, or prevent an important NPC from dying). This is done by listening to the `EntityDeathRequestEvent` and setting its `IsCancelled` property to true.

```csharp
public class ExtraLifeSystem : EntitySystem
{
    public override void OnSceneLoad()
    {
        // Subscribe to death requests ONLY for entities that have an ExtraLifeComponent
        Events.Subscribe<ExtraLifeComponent, EntityDeathRequestEvent>(OnDeathRequest);
    }

    private void OnDeathRequest(Entity<ExtraLifeComponent> entity, EntityDeathRequestEvent ev)
    {
        // Prevent the entity from dying
        ev.IsCancelled = true;
        
        // Consume the extra life and heal the entity
        ev.Actor.Remove<ExtraLifeComponent>();
        ev.Actor.Set(new Health { Value = 100 });
    }
}
```

**Important considerations:**

- When cancelling a death request via `EntityDeathRequestEvent`, you must typically fix the underlying condition that caused the death (e.g., restore health), or it might be requested again on the next frame.
- You can also intercept `ForcedEntityDeathRequestEvent` (used for critical cleanup routines). However, intercepting a forced death by setting `CancelAndRaiseFatalError = true` will intentionally crash the application. This should only be used as a guard clause when a system fundamentally cannot afford for an entity to be cleaned up.
- You cannot prevent `EntityDeathEvent`, as it represents an irrevocable notice that the entity has been marked for death.
