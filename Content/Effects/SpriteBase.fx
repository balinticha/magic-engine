#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// 1. The Texture Variable (SpriteBatch automatically fills this)
Texture2D SpriteTexture;

// 2. The Sampler State (How to read the texture)
sampler2D TextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

// --- MISSING PART END ---

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    // 1. Sample the texture (sRGB)
    float4 color = tex2D(TextureSampler, input.TexCoord);

    // 2. Apply Vertex Color (sRGB)
    color *= input.Color;

    // 3. LINEARIZE (The Fix)
    // Convert sRGB -> Linear before blending
    // Only apply to RGB. Leave Alpha linear.
    color.rgb = pow(color.rgb, 2.2);

    // 4. PREMULTIPLY
    // Multiply RGB by Alpha for correct blending in the buffer
    color.rgb *= color.a;

    return color;
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};