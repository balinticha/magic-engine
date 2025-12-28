# Camera System Documentation

## Overview

The Camera System in MagicEngine uses a specialized "Low-Res + Sub-Pixel Smoothing" approach.
It is designed for games using low-resolution pixel art (e.g., 640x360) that need to be upscaled to modern screens without jittering or pixel shimmering.

## Architecture (For Engine Developers)

### 1. CameraSystem

The `CameraSystem` class is a lightweight container residing in `Engine/ECS/Core/Camera`.
*   **Data**: `Vector2 Position` (The absolute world position the camera is looking at).
*   **Role**: It acts as the "Target" for rendering logic. It does not perform updates itself (no `Update` method).

### 2. Rendering Logic (`MagicGame.Draw`)

The camera handling is hardcoded into the render loop in `MagicGame.cs` to ensure tight integration with the dual-pass rendering stability.

1.  **Integer Snapping**:
    *   The primary sprite batch is drawn with a View Matrix created from `Round(CameraPosition)`.
    *   This ensures all sprites line up perfectly with the "Virtual Pixel Grid", preventing "pixel crawling" artifacts.
2.  **Sub-Pixel Upscaling**:
    *   The `GraphicsManager.ScreenTarget` (High Res) draws the low-res result.
    *   It applies a slight offset based on the *remainder* of the float position (`Position - Round(Position)`).
    *   This allows the entire low-rez image to move smoothly across the screen at high refresh rates, providing fluid movement while maintaining pixel-perfect asset alignment relative to each other.

## Usage Guide (For Engine Users)

### 1. Controlling the Camera

The camera does not move by itself. You must create a System to update `CameraSystem.Position`.

```csharp
public class CameraFollowSystem : EntitySystem
{
    // Inject the engine's camera system
    [Dependency] private readonly CameraSystem _camera;
    [Dependency] private readonly SessionManager _session;

    public override void Update(Timing t)
    {
        // 1. Get the player
        if (_session.GetControlledEntity(out var player) && player.TryGet<Position>(out var pos))
        {
            // 2. Smoothly damp towards target
            _camera.Position = Vector2.Lerp(_camera.Position, pos.Comp.Value, t.Alpha * 5f);
        }
    }
}
```

### 2. Screen Space vs World Space

*   **World Space**: The actual coordinates in the game world (referenced by `Position` components).
*   **Screen Space**: 0,0 is the top-left of the monitor.

Because of the integer snapping and upscaling, converting mouse coordinates to world coordinates requires using the `GraphicsManager` scaling factors. *See `InputSystem` helper methods if available, or transform standard MouseState using the View Matrix.*
