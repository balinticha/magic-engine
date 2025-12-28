# Debug Command System Documentation

## Overview

The Debug Command system in MagicEngine provides a runtime console interface for developers and users to interact with the game engine. It allows for executing commands to manipulate game state, spawn entities, debug systems, and configure settings without restarting the game.

The system is managed by `CommandManager`, which parses input and dispatches valid commands to their respective handlers.

## Architecture (For Engine Developers)

The system is built around the `IConsoleCommand` interface and the `ConsoleCommand` base class.

### 1. IConsoleCommand

The raw interface that all commands must implement.

*   `Name`: The string used to invoke the command in the console (case-insensitive).
*   `Description`: Help text describing what the command does and its arguments.
*   `Execute(string[] args)`: The method called when the command is invoked. Returns a string to be printed to the console.

### 2. ConsoleCommand

A convenience base class (`MagicEngine.Engine.Base.Debug.Commands.ConsoleCommand`) that inherits from `IConsoleCommand`. It is highly recommended to inherit from this class as it provides:

*   **Automatic Injection**: Core system references are automatically populated:
    *   `SceneManager`
    *   `World` (DefaultEcs)
    *   `PhysicsWorld` (Aether.Physics2D)
    *   `EventManager`
    *   `PrototypeManager`
    *   `CameraSystem`
    *   `SystemManager`
    *   `PostProcessingManager`
*   **Helper Methods**:
    *   `TryGetEntityById(string id, out Entity entity)`
    *   `TryParseNumber<T>`
    *   Logging shorthands: `Log`, `D` (Debug), `R` (Release), `V` (Verbose).

### Creating a Custom Command

To add a new debug command:

1.  **Create a Class**: Create a new class ensuring it inherits from `MagicEngine.Engine.Base.Debug.Commands.ConsoleCommand` (or implements `IConsoleCommand`).
2.  **Implement Abstract Members**:
    *   `Name`: The command keyword (e.g., "godmode").
    *   `Description`: A short usage guide.
    *   `Execute`: The logic to run.
3.  **Registration**: You do **not** need to manually register the command. The `CommandManager` uses reflection to automatically find and instantiate all non-abstract classes implementing `IConsoleCommand` in the assembly.

#### Example Implementation

```csharp
using MagicEngine.Engine.Base.Debug.Commands;

public class HealCommand : ConsoleCommand
{
    public override string Name => "heal";
    public override string Description => "Heals the player. Usage: heal [amount]";

    public override string Execute(string[] args)
    {
        int amount = 100;
        if (args.Length > 0 && !int.TryParse(args[0], out amount))
        {
             return "Invalid amount.";
        }

        // Access the player entity (implementation depends on your game logic)
        // ... healing logic ...

        return $"Player healed by {amount}.";
    }
}
```

### Dependency Injection

The `CommandManager` supports dependency injection for your custom commands beyond the standard `ConsoleCommand` properties.

*   **[Dependency] Attribute**: specific fields marked with `[Dependency]` will be resolved using the `SystemManager`.

```csharp
public class MyCommand : ConsoleCommand
{
    [Dependency] private readonly MyCustomSystem _mySystem;
    // ...
}
```

## Usage Guide (For Engine Users)

### Runtime Controls

*   **Open Console**: Press `F2` (or the default console open key) to open the debug console overlay.
*   **Execute**: Type the command name followed by arguments separated by spaces, then press `Enter`.
*   **Scroll**: Use `PageUp` / `PageDown` or mouse wheel to scroll through history (if supported by UI).

### Built-in Commands

Here is a list of standard commands available in the engine:

| Command | Arguments | Description |
| :--- | :--- | :--- |
| `help` | `[page]` | Lists all available commands. |
| `spawn` | `<ProtoID> <X> <Y>` | Spawns an entity from a prototype at the given coordinates. |
| `setscene` | `<SceneID>` | Switches the active scene (e.g., "MainMenu", "Game"). |
| `add_component` | `<EntityID> <CompType>` | Adds a component to an entity. |
| `remove_component` | `<EntityID> <CompType>` | Removes a component from an entity. |
| `despawn` | `<EntityID>` | Destroys an entity. |
| `set_game_speed` | `<Multiplier>` | Sets global time scale (1.0 is normal). |
| `pause` | | Toggles game pause state. |
| `ppconfig` | | Toggles the Post-Processing configuration overlay. |
| `weld_attach` | `<EntA> <EntB>` | Creates a physics weld joint between two entities. |
| `weld_detach` | `<EntA> <EntB>` | Removes a weld joint. |
| `exit` | | Closes the application. |
| `crash` | | Intentionally crashes the game (for testing error handling). |

*Note: Entity IDs are typically in the format `WorldId:EntityId` (e.g., `1:105E`) in debug overlays. Use these as parameters, without the "E" part.*
