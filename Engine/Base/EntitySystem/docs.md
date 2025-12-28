# Entity System Documentation

## Overview

The Entity System in MagicEngine is the core framework for game logic execution. It integrates with `DefaultEcs` to provide a structured way to manage and run "Systems" — classes that operate on entities or manage specific game functionality.

The system is orchestrated by the `SystemManager`, which handles the discovery, initialization, dependency injection, and execution loop of all `EntitySystem` instances.

## Architecture

### 1. SystemManager

The `SystemManager` is the central hub. It:
*   Automatically discovers all classes inheriting from `EntitySystem` using reflection.
*   Instantiates them and injects core references (`World`, `SceneManager`, `PhysicsWorld`, etc.).
*   Resolves inter-system dependencies via the `[Dependency]` attribute.
*   Organizes systems into **Execution Buckets** to control update order.
*   Runs the game loop, calling `Update()` or `Draw()` on systems in their respective buckets.

### 2. EntitySystem

The abstract base class for all game systems. It provides:
*   **Core Access**: `World` (ECS), `PhysicsWorld`, `EventManager`, `PrototypeManager`, `Camera`.
*   **Lifecycle Methods**:
    *   `Initialize()`: Run once at startup.
    *   `OnSceneLoad()`: Run when a scene becomes active.
    *   `OnSceneUnload()`: Run when a scene is deactivated.
    *   `Update(Timing timing)`: Run every frame (frequency depends on bucket).
    *   `Draw(float deltaTime, SpriteBatch sb)`: Run during the render pass.
*   **Logging Helpers**: `Log()`, `D()` (Debug), `V()` (Verbose), `AssertFailure()`.

### 3. Execution Buckets

Systems are grouped into "Buckets" determined by the `[UpdateInBucket]` attribute. These buckets dictate *when* in the frame loop the system runs.

| Bucket | Loop Type | Use Case |
| :--- | :--- | :--- |
| `First` | Variable | Logic that must run absolutely first (e.g., time keeping). |
| `Input` | Variable | Input polling. |
| `PreUpdate` | Fixed | Preparing for the main physics/logic step. |
| `Update` | Fixed | **Default.** Standard game logic, AI, movement. |
| `PostPhysics` | Fixed | Logic that reacts to physics resolution (e.g., collision events). |
| `LateUpdate` | Variable | Camera positioning, smoothing logic that needs final positions. |
| `Cleanup` | Variable | Entity despawning, state resetting. |
| `Render` | Draw | Drawing sprites, UI, debug lines (runs `Draw` instead of `Update`). |

## Usage Guide

### Creating a New System

To create a new system, inherit from `EntitySystem`.

#### Basic Example
```csharp
using MagicEngine.Engine.Base.EntitySystem;
using DefaultEcs;

namespace MagicEngine.Systems;

public class PlayerMovementSystem : EntitySystem
{
    private EntitySet _players;

    public override void Initialize()
    {
        // Cache your EntitySet here for performance
        _players = World.GetEntities()
            .With<PlayerTag>()
            .With<Position>()
            .With<Velocity>()
            .AsSet();
    }

    public override void Update(Timing timing)
    {
        // Iterate over entities
        foreach (var entity in _players.GetEntities())
        {
            ref var pos = ref entity.Get<Position>();
            ref var vel = ref entity.Get<Velocity>();
            
            pos.Value += vel.Value * timing.DeltaTime;
        }
    }
}
```

### Controlling Execution Order

To change when your system updates, use the `[UpdateInBucket]` attribute. If omitted, it defaults to `ExecutionBucket.Update`.

```csharp
[UpdateInBucket(ExecutionBucket.LateUpdate)]
public class CameraSystem : EntitySystem
{
    // This will run after physics and standard logic
    public override void Update(Timing timing) { ... }
}
```

### Dependency Injection

If your system needs to call methods on another system, use the `[Dependency]` attribute. The `SystemManager` will automatically inject the instance.

```csharp
public class CombatSystem : EntitySystem
{
    // Automatically populated by SystemManager
    [Dependency] private readonly AudioSystem _audioSystem;

    private void PlayHitSound()
    {
         _audioSystem.PlaySfx("punch");
    }
}
```
**Note**: Dependencies are injected *before* `Initialize()` is called, so they are safe to use during initialization.

### Drawing

If your system needs to render (e.g., a HUD or Debug Drawer), use the `Render` bucket and override `Draw`.

```csharp
[UpdateInBucket(ExecutionBucket.Render)]
public class HudSystem : EntitySystem
{
    public override void Draw(float deltaTime, SpriteBatch spriteBatch)
    {
        spriteBatch.DrawString(Font, "Health: 100", new Vector2(10, 10), Color.White);
    }
}
```
