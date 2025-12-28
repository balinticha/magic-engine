# Positioning System Documentation

## Overview

The Positioning System in MagicEngine handles how entities move in space, how their position is synchronized with the Physics engine, and how that position is smoothed for rendering (Interpolation).

It separates the concept of **Logic Position** (`Position` component) from **Render Position** (`RenderPosition` component) to allow for fixed-timestep logical updates and variable-timestep smooth rendering.

## Architecture (For Engine Developers)

### 1. Components

*   `Position`: The "Truth" of where an entity is in the logic world (Vector2).
*   `Velocity`: The rate of change of position (Vector2).
*   `RenderPosition`: The calculated position for drawing sprites. Derived from `Position` but often interpolated.
*   `PreviousPosition`: Stores the `Position` from the *start* of the current logical tick. Used for interpolation.
*   `SetPositionRequest`: A one-frame component used to safely teleport entities without breaking physics or interpolation.

### 2. The Logic Loop (Fixed Update)

1.  **Snapshotting (`PositionSystem`)**: At the start of the fixed tick (`PreUpdate`), the current `Position.Value` is copied to `PreviousPosition.Value`.
2.  **Request Handling (`ProcessPositionRequestSystem`)**: Reads `SetPositionRequest`. If the entity has a physics body, it teleports the Body. If not, it sets `Position`.
    *   *Note*: This handling is currently manual and might need better integration into the automated flow.
3.  **Physics Step**: The Aether.Physics2D world simulates forward.
4.  **Sync (`PostPhysicsSyncSystem`)**: (Located in `Scene`) Copies the new Physics Body position back to the `Position` component.

### 3. The Render Loop (Variable Update)

1.  **Interpolation (`InterpolateSystem`)**:
    *   **Constraint**: Runs in `PreRender`.
    *   **Logic**: `Vector2.Lerp(PreviousPosition, Position, Alpha)`.
    *   **Alpha**: calculated by the game loop, representing how far we are between two fixed ticks.
    *   **Result**: Writes to `RenderPosition`.
2.  **Fallback (`SyncRenderPositionSystem`)**:
    *   If an entity has `Position` and `RenderPosition` but *no* `PreviousPosition` (i.e. we don't want interpolation for it), this system simply copies `Position` directly to `RenderPosition` to ensure it still draws correctly.

## Usage Guide (For Engine Users)

### 1. Moving an Entity

**Physics Entities**:
**NEVER, EVER** modify `Position` directly if the entity has a `PhysicsBodyComponent`. Apply forces or set velocity on the Body.

**Non-Physics Entities**:
You can modify `Position.Value` directly in your `Update()` systems.

```csharp
ref var pos = ref entity.Get<Position>();
pos.Value += new Vector2(1, 0); // Move right
```

### 2. Teleporting (Safe Method)

To instantly move an entity (especially a physics one) and reset interpolation (so it doesn't "whoosh" to the new spot), use `SetPositionRequest`.

```csharp
entity.Set(new SetPositionRequest 
{ 
    RequestPosition = newTargetPos,
    RequestVelocityChange = Vector2.Zero 
});
```

### 3. Enabling Interpolation

To make an entity move smoothly (essential for player characters or fast-moving objects):
1.  Ensure `Position` exists (is an innate component - should always be present)
2.  Add `RenderPosition`
3.  Add `PreviousPosition` (This is the flag that enables the `InterpolateSystem`).

If `PreviousPosition` is missing, the entity will snap to grid/tick positions, which may look jittery on high refresh rate monitors.

### 4. Getting Screen Coordinates

Always use `RenderPosition` when drawing or determining where something is *visually* on screen.

```csharp
// In a Draw system
var visualPos = entity.Get<RenderPosition>().Value;
spriteBatch.Draw(..., visualPos, ...);
```
