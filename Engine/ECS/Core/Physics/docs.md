# Physics System Documentation

## Overview

The Physics System integrates the `Aether.Physics2D` library (a Box2D fork) with the `DefaultEcs` entity system. 

It handles the creation and destruction of physics bodies, synchronizes their positions with the ECS world, and dispatches collision events to the game logic.

## Architecture (For Engine Developers)

### 1. Scaling (`PhysicsSystem.ToPhysics` / `.ToECS`)

Box2D is tuned for objects between 0.1 and 10 meters. Game values (pixels) are typically much larger.
*   **PixelsPerMeter**: `100f`
*   **MetersPerPixel**: `0.01f`
*   **Rule**: Always wrap values when moving between ECS (Pixels) and Physics (Meters) using `PhysicsSystem` helper methods.

### 2. Bridging Systems

*   **`PhysicsBodyCreationSystem`**:
    *   Watches for entities with `RectangleColliderComponent` (or others) that *lack* a `PhysicsBodyComponent`.
    *   Creates the Body in the Aether.Physics2D World.
    *   Sets up the Shape, Fixture, Density, and Collision Rules.
    *   Adds `PhysicsBodyComponent` to the entity.
*   **`PhysicsBodyDeletionSystem`**:
    *   Cleans up the Aether Body when an Entity is destroyed or the component is removed.

### 3. Synchronization Loop

1.  **ECS -> Physics (`PrePhysicsSyncSystem`)**:
    *   (Currently implied/manual in some places, check `ProcessPositionRequestSystem` from Positioning docs).
    *   Ensures that if we teleport an entity in ECS, the Physics Body moves too.
2.  **Physics Simulation**:
    *   Managed by `MagicGame` or global update loop (Fixed Step).
3.  **Physics -> ECS (`PostPhysicsSyncSystem`)**:
    *   Reads the new `Body.Position` and `Body.LinearVelocity`.
    *   Updates the `Position` and `Velocity` components on the Entity.
    *   **Crucial**: This is why you should never set `Position` manually on a physics object—it will be overwritten by this system in the next frame.

### 4. Collisions

*   **Events**: collisions generate `CollisionEvent`s.
*   **Filters**: managed via `CollisionFilterComponent`.
*   **Dispatch**: `PostPhysicsSyncSystem` iterates contacts and floods the `EventManager` with directed events.

## Usage Guide (For Engine Users)

### 1. Creating a Physics Object

To make an entity physically simulated, add a collider and physics material component.

```yaml
- id: Crate
  components:
    - Position: { x: 50, y: 50 }
    - RectangleColliderComponent:
        width: 32
        height: 32
        offset: { x: 0, y: 0 }
    - PhysicsMaterialComponent:
        density: 1.0
        friction: 0.5
        restitution: 0.1 # Bounciness
```

### 2. Moving Physics Objects

**Do NOT** modify `Position.Value` directly.

**Correct Way (Forces/Velocity):**
```csharp
ref var body = ref entity.Get<PhysicsBodyComponent>().Body;
body.ApplyLinearImpulse(new Vector2(0, -10)); // Jump!
```

**Correct Way (Teleport):**
Use `SetPositionRequest` (see Positioning docs).

### 3. Detecting Collisions

Subscribe to `CollisionEvent`.

```csharp
public override void OnSceneLoad()
{
    Events.Subscribe<PlayerTag, CollisionEvent>(OnPlayerCollide);
}

private void OnPlayerCollide(Entity<PlayerTag> player, CollisionEvent ev)
{
    // Check what we hit
    var otherEntity = ev.EntityB; // Relative to EntityA (Player)
    
    if (otherEntity.Has<Spikes>())
    {
        // Ouch
    }
}
```

### 4. Filtering (Who hits who?)

Use `CollisionFilterComponent` to put objects in categories.

```yaml
- id: Ghost
  components:
    - CollisionFilterComponent:
        category: "Ghost"       # Only collides with "Magic"
        collidesWith: "Magic"   # Passes through "Walls"
```
*(Note: Category names usually map to an Enum defined  game code).*
