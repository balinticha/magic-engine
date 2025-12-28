# Input System Documentation

## Overview

The Input System provides an abstraction layer between raw hardware input (Keyboard, Mouse) and game logic. 
Instead of checking `if (Keyboard.IsKeyDown(Keys.W))`, game systems check `if (Input.IsPressed(InputAction.MoveUp))`. This allows for rebindable controls via configuration files.

## Architecture (For Engine Developers)

### 1. InputManagerSystem

*   **Bucket**: `ExecutionBucket.Input` (Runs early in the frame).
*   **Initialization**: Reads `Configs/bindings.yml` using `YamlDotNet` to populate the binding map.
*   **Update Loop**: 
    1.  Polls `Keyboard.GetState()` and `Mouse.GetState()`.
    2.  Iterates all `Action -> Binding` pairs.
    3.  Calculates `InputState` (JustPressed, Down, JustReleased, Up) by comparing current vs previous frame.
*   **Public API**:
    *   `GetActionState(InputAction)`: Returns full state enum.
    *   `IsPressed(InputAction)`: Returns true if Down or JustPressed.

### 2. Data Structures

*   `InputAction` (Enum): The vocabulary of actions the game understands (e.g., `MoveUp`, `PrimaryInteract`).
*   `InputBinding` (Abstract Class): Represents a hardware source.
    *   `KeyBinding`: Maps to a keyboard key.
    *   `MouseButtonBinding`: Maps to Left/Right/Middle mouse buttons.

## Usage Guide (For Engine Users)

### 1. Checking Input

In your `Update` systems, inject `InputManagerSystem`.

```csharp
public class PlayerController : EntitySystem
{
    [Dependency] private readonly InputManagerSystem _input;

    public override void Update(Timing t)
    {
        var velocity = Vector2.Zero;

        if (_input.IsPressed(InputAction.MoveUp))    velocity.Y -= 1;
        if (_input.IsPressed(InputAction.MoveDown))  velocity.Y += 1;
        
        if (_input.GetActionState(InputAction.PrimaryInteract) == InputState.JustPressed)
        {
            Attack();
        }
    }
}
```

### 2. Defining Bindings

Bindings are defined in `Configs/bindings.yml`. You can open this file to change controls without recompiling.

```yaml
bindings:
  - action: MoveUp
    type: Key
    value: W
  - action: MoveDown
    type: Key
    value: S
  - action: PrimaryInteract
    type: Mouse
    value: Left
```

### 3. Adding New Actions

To add a new action (e.g., "Reload"):

1.  Open `InputManagerSystem.cs`.
2.  Add `Reload` to the `InputAction` enum.
3.  Add the default binding in `bindings.yml`:
    ```yaml
    - action: Reload
      type: Key
      value: R
    ```
