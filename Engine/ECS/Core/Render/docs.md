# Render System Documentation

## Overview

The Render System in MagicThing uses `DefaultEcs` to draw entities during the game's draw cycle. Unlike standard `Update` systems, rendering systems run in the `ExecutionBucket.Render` stage, which corresponds to the MonoGame `Draw` call.

The architecture decouples the **Logic Position** (`Position` component) from the **Render Position** (`RenderPosition` component). This rendering layer solely consumes `RenderPosition`.

## Architecture (For Engine Developers)

### 1. SpriteDrawSystem

The `SpriteDrawSystem` has been overhauled to support efficient batching, sorting, and culling.

*   **Bucket**: `ExecutionBucket.Render`

*   **Query**:
    *   Matches entities with `RenderPosition` AND `Sprite`.
    *   OR matches entities with `RenderPosition` AND `RenderBounds` AND `Material` (without `Sprite`).

#### Rendering Pipeline

1.  **Culling**: The system calculates the world-space view bounds (plus a margin) and performs a fast culling check. Entities outside the view are skipped.
2.  **Queueing**: Visible entities are added to a `_renderQueue` list as `RenderItem` structs. This decouples the iteration order from the draw order.
3.  **Sorting**: The render queue is sorted to minimize state changes and ensure correct visual stacking.
    *   **Sort Order**:
        1.  `Layer` (int) - Explicit draw order.
        2.  `Z` (float) - Screen Y position + `SortOffset` (for top-down pseudo-depth).
        3.  `Effect` (HashCode) - Group by Shader.
        4.  `BlendMode` (int) - Group by Blending Mode (AlphaBlend, Additive, etc.).
        5.  `ParamHash` (int) - Group by Material Parameters.
4.  **Batching**: The system iterates the sorted queue and submits draw calls. It automatically manages `SpriteBatch.Begin()` and `SpriteBatch.End()` calls.
    *   A new batch is started **only** when the `Effect` or `ParamHash` changes.
    *   Material parameters are applied to the Effect just before the batch starts.

### 2. DrawPrimitiveSystem

A system for drawing simple shapes, primarily for debug or prototyping (powered by `MonoGame.Extended`).

*   **Bucket**: `ExecutionBucket.Render`
*   **Query**: Matches `RenderPosition` AND (`DrawRectangle` OR `DrawCircle`).
*   **Usage**: Useful for visualizing hitboxes or triggers.

---

## Render Components

### Core Components

*   **`Sprite`**: The visual data.
    *   `Texture`: The `Texture2D` to draw.
    *   `Color`: Tint color.
    *   `Layer`: Major sort layer (background, foreground, etc.).
    *   `SortOffset`: Fine-tuning for Y-sorting (e.g., placing a sprite's "feet" anchor).
*   **`RenderPosition`**: The screen-space coordinates (interpolated from physics position).

*   **`Material`**: (Optional) specific shader override and parameters.
*   **`RenderBounds`**: (Alternative to Sprite) Defines the size/anchor for material-only entities.
    *   `Width`, `Height`: Size of the render quad.
    *   `Anchor`: Pivot point (0..1).
    *   `Layer`, `SortOffset`: Sorting parameters (same as Sprite).

### Material Component

The `Material` component allows entities to use custom shaders and pass uniform parameters to them.

```csharp
[Component]
public struct Material
{
    public Effect Effect; // The shader
    public Dictionary<string, object> Parameters; // Uniform values
    public MaterialBlendMode BlendMode; // Blending mode (Default: AlphaBlend)
}
```

*   **BlendMode**: Controls how the sprite/result is blended with the background.
    *   `AlphaBlend` (0): Standard transparency.
    *   `Additive` (1): Adds colors (glowing effects).
    *   `NonPremultiplied` (2): For textures with straight alpha.
    *   `Opaque` (3): No transparency, overwrites background.

*   **Caching**: The component calculates a `CachedHash` of the parameters to allow the renderer to quickly compare materials for batching without deep equality checks.
*   **UpdateHash()**: Called automatically when setting the `Parameters` property or using `SetParameter()`.

---

## Usage Guide (For Engine Users)

### 1. Displaying a Sprite

To make an entity visible, attach the `Sprite` component.

```yaml
- id: Box
  components:
    - Position: { x: 100, y: 100 }
    - Sprite:
        texture: "Sprites/box"
        color: "White"
        layer: 0
```

### 2. Using Materials (Shaders)

To apply a custom shader (e.g., a flash effect, outline, or color swap) to a sprite, add the `Material` component.

**YAML Example:**
```yaml
- id: MagicOrb
  components:
    - Position: { x: 200, y: 200 }
    - Sprite:
        texture: "FX/Orb"
    - Material:
        effect: "Effects/GlowShader"
        parameters:
            intensity: 1.5
            color: { r: 0, g: 1, b: 0, a: 1 }
```

**C# Example:**
```csharp
entity.Set(new Material { Effect = myEffect });
ref var mat = ref entity.Get<Material>();
mat.SetParameter("Intensity", 2.0f);
```

### 3. Material-Only Entities (Procedural Effects)

You can render an entity without a texture (e.g., for procedural shaders) by using the `RenderBounds` component instead of `Sprite`. The renderer will use a 1x1 white pixel texture stretched to the specified bounds.

**YAML Example:**
```yaml
- id: ProceduralFire
  components:
    - Position: { x: 300, y: 300 }
    - RenderBounds:
        width: 64
        height: 64
        anchor: { x: 0.5, y: 0.5 }
        layer: 1
    - Material:
        effect: "Effects/FireShader"
        parameters:
            timeScale: 1.0
```

### 4. Global Shader Parameters

The renderer automatically injects certain global parameters into every active shader if the shader has a matching parameter name.

*   `Time` (float): The total game time in seconds.

### 5. Performance Tips

*   **Batching**: The renderer tries to group objects with the same Effect and Parameters. Switching materials breaks the batch and incurs a draw call cost. Use the same Material instance or distinct Materials with identical parameters where possible for large groups of objects.
*   **Textures**: Currently, switching textures does *not* break the batch (thanks to `SpriteBatch`), but using many different textures across many different materials can still be heavy.
