#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

sampler TextureSampler : register(s0);

// Parameters
float Exposure; // Default 1.0
float IgnDitherStrength; // Default 0.0 (Disabled) or e.g. 1.0/255.0
float2 ScreenSize; 

// ----------------------------------------------------------------------------------
// Helper Functions
// ----------------------------------------------------------------------------------

// ACES Tone Mapping (Narkowicz fit)
float3 ACESFilm(float3 x)
{
    float a = 2.51f;
    float b = 0.03f;
    float c = 2.43f;
    float d = 0.59f;
    float e = 0.14f;
    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

// ----------------------------------------------------------------------------------
// Pixel Shader
// ----------------------------------------------------------------------------------

float4 MainPixelShader(float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 texColor = tex2D(TextureSampler, uv);
    float3 hdrColor = texColor.rgb;

    // 1. Exposure
    hdrColor *= Exposure;

    // 2. Tone Mapping (ACES)
    float3 mapped = ACESFilm(hdrColor);

    // 3. Gamma Correction (Linear -> sRGB)
    // Gamma 2.2 approximation: pow(color, 1.0 / 2.2)
    mapped = pow(mapped, 1.0 / 2.2);

    // 4. Dithering (Interleaved Gradient Noise)
    // Avoids banding in dark areas
    if (IgnDitherStrength > 0.001) // Optimization: skip if disabled
    {
        // Calculate pixel position from UV
        float2 pixelPos = uv * ScreenSize;
        
        // Magic numbers for Interleaved Gradient Noise
        float3 magic = float3(0.06711056, 0.00583715, 52.9829189);
        float ign = frac(magic.z * frac(dot(pixelPos, magic.xy)));
        
        // Apply dithering: (ign - 0.5) is range [-0.5, 0.5]
        mapped += (ign - 0.5) * IgnDitherStrength;
    }

    // Output with Alpha 1.0 (Opaque) - Screen is always opaque defined
    return float4(mapped, 1.0);
}

technique BasicColor
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPixelShader();
	}
}
