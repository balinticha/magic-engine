# Shader Writing Guide

This document details how to write HLSL shaders for the **MagicThing** rendering engine.

## 1. Input Layout

Shaders in this engine are consumed by the standard `SpriteBatch`. The vertex processing is handled by the engine, so your `VertexShader` is usually standard. The main customization happens in the **Pixel Shader**.

### 1.1 Standard Vertex Inputs
The engine sends vertices with the `VertexPositionColorTexture` layout.

| Field | Type | Description |
| :--- | :--- | :--- |
| `POSITION` | `float4` | Screen-space position. |
| `COLOR0` | `float4` | Vertex color (usually `Color.White` unless tinted by the sprite logic). |
| `TEXCOORD0` | `float2` | Texture coordinates (0,0 to 1,1). |

### 1.2 System Parameters
The system automatically injects these parameters if they are declared in your shader:

| Parameter Name | HLSL Type | Description |
| :--- | :--- | :--- |
| `Time` | `float` | Simple game time in seconds. Useful for animation. |
| `TextureSampler` | `sampler` | The main texture (or white pixel) being drawn. |

### 1.3 Material Parameters
Any parameter defined in your `Material.cs` (YAML) `Parameters` dictionary is injected if the name matches.
Supported Types: `float`, `int`, `float2`, `float3`, `float4`, `Texture2D`.

#### CRITICAL RULE
ALWAYS linearize color material AND texture parameters. The engine provides them in sRGB (0->1)

```
// BAD
float4 color = tex2D(TextureSampler, input.TexCoord);
float4 tint = ColorParam;

// GOOD
float4 color = tex2D(TextureSampler, input.TexCoord);
color.rgb = pow(color.rgb, 2.2); // Linearize texture

float4 tint = ColorParam;
tint.rgb = pow(tint.rgb, 2.2); // Linearize the C# color parameter
```

### What you work with
Shaders are rendered in the texel layer. In the texel layer, the entire RenderTarget is around 640 pixels wide and 300-ish pixels tall.
This is the entire screen. In your RenderBounds component, the defined height and width value IS precisely the amount of pixels you 
can work with for a given effect. This is not something you specifically have to implement in your shader or something that'd require 
extra effort from the shader's part - this is all handled by the rendering pipeline - 
but it's something to keep in mind while designing sizes in the shader.

## 2. Output Format (CRITICAL)

### HDR Ranges
- We use a HDR range from 1 to 10:
- Surfaces: 0 - 0.95
- Weak glowing light / firefly: 1 - 2
- Moderate light / lightbulb: 2 - 3
- Strong light / fireball: 4 - 6 
- Extreme light / sun: 10+

Ensure the shader outputs the proper HDR ranges so it renders correctly. Additionally:  
The engine rendering pipeline uses **Premultiplied Alpha**.
All custom shaders **MUST** output colors in premultiplied format.

### The Rule
> **Multiply your RGB values by your Alpha value before returning.**

### Why?
-   `AlphaBlend` mode expects `One, InverseSourceAlpha`. If you don't premultiply, transparency will look "ghostly" or wrong (RGB typically stays bright while A drops).
-   `Additive` mode uses `One, One` (Linear Dodge). This blends perfectly with premultiplied inputs.

### Example (Correct)
```hlsl
float4 PixelShader(float4 pos : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 texColor = tex2D(TextureSampler, uv);
    
    // Calculate your fancy effects
    float3 glowingColor = float3(1.0, 0.5, 0.0);
    float glowAlpha = 0.5;

    // ... calculation ...

    // FINAL STEP: Premultiply!
    return float4(glowingColor * glowAlpha, glowAlpha) * color;
}
```

### Example (Incorrect - Do NOT do this)
```hlsl
// WRONG: RGB is not multiplied by Alpha
return float4(glowingColor, glowAlpha) * color; 
```

## 3. Blending Modes

You can control how your shader blends with the scene via the `BlendMode` property in the `Material` component.

## 4. Boilerplate Template

Copy this to start a new effect:

```hlsl
#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

sampler TextureSampler : register(s0);
float Time;

// Custom Params
float Intensity;

float4 MainPixelShader(float4 pos : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    // Logic here
    float4 result = float4(1, 0, 0, 0.5); // Red at 50% opacity
    
    // Premultiply
    result.rgb *= result.a;
    
    return result * color;
}

technique BasicColor
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPixelShader();
	}
}
```
