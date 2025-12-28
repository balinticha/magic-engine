# PostProcessing System Documentation

## Overview

The PostProcessing system in MagicThing allows for applying visual effects to the game's rendered output. It supports a multi-pass pipeline where effects can be chained together.

The system is managed by `PostProcessingManager` and uses a "Ping-Pong" rendering strategy, swapping between two render targets (`dest` and `swap`) to apply effects sequentially.

## Architecture (For Engine Developers)

The pipeline is divided into two distinct processing layers, controlled by the `EffectType` enum:

### 1. TexelLayer
*   **When**: Applied *before* the game is upscaled to the screen resolution.
*   **Target**: The internal low-resolution `GameRenderTarget`.
*   **Use Case**: Pixel-art consistent effects (e.g., color correction, palette swapping, dithering).
*   **Result**: Changes strictly adhere to the game's pixel grid.

### 2. PixelLayer
*   **When**: Applied *after* the game has been upscaled to the final screen resolution.
*   **Target**: The high-resolution `ScreenTarget`.
*   **Use Case**: High-fidelity effects that "break" the pixel grid (e.g., smooth bloom, CRT scanlines, chromatic aberration, sub-pixel blur).

### Creating a Custom Effect

To add a new post-processing effect:

1.  **Create a Class**: Inherit from `MagicThing.Engine.Base.Shaders.PostProcessing.PostProcessStep`.
2.  **Constructor**:
    *   Set the `Type` property to either `EffectType.TexelLayer` or `EffectType.PixelLayer`.
    *   Load or assign the Monogame `Effect` instance.
3.  **Implement Apply**:
    ```csharp
    public override void Apply(SpriteBatch sb, Texture2D source, RenderTarget2D destination)
    {
        // 1. Set Effect Parameters
        myEffect.Parameters["Param1"]?.SetValue(Value);
        
        // Helper to pass ScreenSize (common uniform)
        SetStandardParameters(myEffect, destination); // Sets "ScreenSize" to destination dimensions

        // 2. Draw
        sb.GraphicsDevice.SetRenderTarget(destination);
        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, 
            DepthStencilState.Default, RasterizerState.CullNone, myEffect);
        
        sb.Draw(source, new Rectangle(0, 0, destination.Width, destination.Height), Color.White);
        sb.End();
    }
    ```
4.  **Register it**: Add your step to the `PostProcessingManager.Effects` list (usually in `MagicGame.Initialize` or `LoadGameContent`).

## Usage Guide (For Engine Users)

### Runtime Controls
*   **Toggle On/Off**: Press `F3` during gameplay to globally enable or disable all post-processing effects.

### Configuration
Most effects expose public properties for runtime tuning. For example, `SimpleBlur` has:
*   `Threshold`: Controls luminance cutoff for the blur.
*   `Intensity`: Controls the strength of the blur.

Example configuration in `LoadGameContent`:
```csharp
var blur = new SimpleBlur(Content.Load<Effect>("Effects/Blur"));
blur.Threshold = 0.6f;
PostProcessingManager.Effects.Add(blur);
```
