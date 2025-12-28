# Session System Documentation

## Overview

The Session System manages the player's persistent identity and control within the game world. It abstracts the notion of "The Player" away from a specific Entity instance, allowing for mechanics like death, respawning, and possessing different bodies.

The core concept is the **Controlled Entity**. The `SessionManager` tracks which Entity currently represents the player.

## Architecture (For Engine Developers)

### 1. SessionManager

The `SessionManager` is a `LateUpdate` system that:
*   Holds a reference to the `_controlledEntity`.
*   Tracks the `ControlledEntityLastPosition` (useful for respawning or camera focus when the entity dies).
*   **API**:
    *   `SetControlledEntity(Entity e)`: Assigns player control to an entity.
    *   `GetControlledEntity(out Entity e)`: Safely retrieves the player entity. Returns false if null or dead.

### 2. Soul Mechanics (`SoulSystem`)

The engine implements a specific gameplay loop regarding player death via `SoulSystem` (running in `Cleanup` bucket):

1.  **Death Detection**: Every frame, it checks if the player currently controls a valid entity.
2.  **Soul Spawning**: If the controlled entity is null or dead, it immediately spawns a "Soul" entity at the last known position.
3.  **Possession**: The `SessionManager` automatically attaches control to this new Soul.
4.  **Cleanup**: If the player switches control *away* from a Soul (e.g., possesses a new body), the old Soul entity is automatically destroyed.

### 3. Early Attach

The `EarlyAttachSystem` (running in `First` bucket) scans for entities with the `[AttachSoulOnLoad]` component.
*   **Use Case**: When loading a level, you can tag a spawn point or a character with this component.
*   **Result**: The system will automatically call `SetControlledEntity` on the first one it finds and then self-destructs (runs once).

## Usage Guide (For Engine Users)

### 1. Possessing an Entity

To give the player control of an entity (e.g., after spawning a vehicle or a new character), use the `SessionManager`.

```csharp
public class PossessionCommand : ConsoleCommand
{
    public override string Execute(string[] args)
    {
        // ... find target entity ...
        
        // Transfer control
        ((SessionManager)SystemManager.GetSystem<SessionManager>()).SetControlledEntity(targetEntity);
        
        return "Possession successful.";
    }
}
```

### 2. Accessing the Player

In your gameplay systems (e.g., Camera, AI, UI), avoid storing your own reference to "The Player." Instead, query the session.

```csharp
public class CameraSystem : EntitySystem
{
    [Dependency] private readonly SessionManager _session;

    public override void Update(Timing t)
    {
        if (_session.GetControlledEntity(out var player))
        {
            // Focus camera on player
            ref var pos = ref player.Get<Position>();
            _camera.LookAt(pos.Value);
        }
    }
}
```

### 3. Setting Initial Spawn

To define which entity the player starts as in a `.yaml` prototype, add the `AttachSoulOnLoad` component.

```yaml
- id: PlayerStart
  components:
    - Position: { x: 0, y: 0 }
    - AttachSoulOnLoad: {} # Player will possess this immediately on load
    - Sprite: { texture: "hero" }
```
