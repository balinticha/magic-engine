# Lifecycle System Documentation

## Overview

The Lifecycle System provides a safe way to destroy entities. Instead of deleting an entity immediately (which might cause crashes if done while a system is iterating over it), this system allows marking entities for deletion, which are then cleaned up safely at the end of the frame.

## Architecture (For Engine Developers)

### 1. DeferredEntityDisposalSystem

*   **Bucket**: `ExecutionBucket.Cleanup` (Runs at the very end of the frame).
*   **Mechanism**:
    1.  Queries all entities with the `MarkedForDeath` component.
    2.  Iterates through them.
    3.  Calls `entity.Dispose()` on each.
    4.  Logs the destroyed IDs (in verbose mode).

### 2. Components

*   `MarkedForDeath`: An empty "Flag" component (marker). Presence indicates the entity should be destroyed this frame.

## Usage Guide (For Engine Users)

### 1. Killing an Entity Safely

When an enemy reaches 0 hp, or a projectile hits a wall, do **not** call `entity.Dispose()` directly in your update loop. This is unsafe.

**Correct Usage**:
Tag the entity with `MarkedForDeath`.

```csharp
// Inside BulletSystem.cs
if (bullet.Intersects(wall))
{
    // Don't kill it yet! Just mark it.
    bulletEntity.Set<MarkedForDeath>();
    
    // Spawn explosion effect immediately if you want
    SpawnExplosion(bulletEntity.Get<Position>().Value);
}
```

### 2. Immediate Disposal (Advanced)

If you are absolutely sure no other system is currently using this entity (e.g., during a scene transition or pure data generation step), you *can* call `entity.Dispose()`. However, for general gameplay logic, always use `MarkedForDeath`.
