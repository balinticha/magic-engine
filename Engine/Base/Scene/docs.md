# Scene System Documentation

## Overview

The Scene System manages the high-level context of the game. A **Scene** in MagicThing is a container for:
*   **ECS World** (`DefaultEcs.World`): Holds all game entities.
*   **Physics World** (`Aether.Physics2D`): Manages physics bodies and simulation.
*   **Event Manager**: Handles scene-local events.
*   **Attached Systems**: Core systems required for the scene to function (like physics sync).

The `SceneManager` is the global registry/switcher that controls which scene is currently active and ensures systems are notified when the scene changes.

## Architecture (For Engine Developers)

### 1. SceneManager

The `SceneManager` is the single source of truth for the "Current Scene". It interacts closely with the `SystemManager`.

*   **Registry**: Stores scenes in a `Dictionary<string, Scene>`.
*   **Switching Logic**:
    1.  User calls `TrySetActive("Level1")`.
    2.  `SystemManager.CallOnSceneUnload()` is triggered (notifies *all* registered `EntitySystem`s).
    3.  `CurrentScene` pointer is updated.
    4.  `SystemManager.CallOnSceneLoad()` is triggered.
*   **Safety**: It prevents deregistering the currently active scene to avoid crashes.

### 2. Scene Struct

The `Scene` object is a lightweight struct acting as a handle to the actual data.
*   **Immutable References**: references to `World`, `PhysicsWorld`, etc., are readonly.
*   **SceneCreationResources**: A helper struct passed during creation containing `GraphicsDevice` and `ContentManager`.

### 3. AttachedSceneSystems

Each `Scene` instance comes with a set of "Attached Systems". These are **not** managed by the global `SystemManager` but are specific to that scene instance.

*   `PhysicsBodyCreationSystem`: Listens for ECS components and creates Box2D bodies.
*   `PhysicsBodyDeletionSystem`: Cleans up Box2D bodies when entities are destroyed.
*   `Pre/PostPhysicsSyncSystem`: Synchronizes transform data between DefaultECS (`Position`, `Rotation`) and Aether.Physics2D bodies each frame.
*   `DebugView`: A Box2D debug renderer for visualizing colliders.
*   **Note**: These systems are instantiated automatically when a `Scene` is created.

## Usage Guide (For Engine Users)

### 1. Creating a Scene

To create a new scene, you typically need to initialize the ECS and Physics worlds and pass them to the `Scene` constructor.

```csharp
// 1. Create contexts
var ecsWorld = new DefaultEcs.World();
var physicsWorld = new nkast.Aether.Physics2D.Dynamics.World(new Vector2(0, 9.8f)); // Gravity
var eventManager = new EventManager();

// 2. Prepare resources (usually passed from Game1)
var resources = new SceneCreationResources(GraphicsDevice, Content);

// 3. Create the scene
var myScene = new Scene(resources, "Level1", ecsWorld, eventManager, physicsWorld);
```

### 2. Registering and Activating

Once created, you must register the scene with the manager and then activate it.

```csharp
// Register
sceneManager.RegsiterScene(myScene);

// Activate
bool success = sceneManager.TrySetActive("Level1");
if (success)
{
    Console.WriteLine("Switched to Level1!");
}
```

### 3. Scene Lifecycle Events

When you switch scenes, your generic `EntitySystem` classes will receive callbacks. Use these to reset state, load specific assets, or subscribe to events.

```csharp
public class CombatSystem : EntitySystem
{
    public override void OnSceneLoad()
    {
        // Subscribe to events valid for this scene
        Events.Subscribe<Health, DamageEvent>(OnDamage);
        
        // Find entities in the NEW world
        _enemies = World.GetEntities().With<EnemyTag>().AsSet();
    }

    public override void OnSceneUnload()
    {
        // Unsubscribe to prevent memory leaks or calling logic on dead entities
        Events.Unsubscribe<Health, DamageEvent>(OnDamage);
    }
}
```

### Important Notes

*   **One Active Scene**: Only one scene is active at a time. The `SystemManager` (which runs your game logic) always runs against the `CurrentScene`.
*   **Persistence**: Scenes are **not** destroyed when you switch away from them. They remain in memory. If you switch back to "Level1", it will be exactly as you left it unless you manually reset it.
*   **Physics Sync**: You do not need to manually move physics bodies. Just change the `Position` component on the Entity, and the `PrePhysicsSyncSystem` will move the Body. Conversely, if physics moves the Body, `PostPhysicsSyncSystem` will update the Entity's `Position`.
