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
float Thickness;
float4 RingColor;

float4 MainPixelShader(float4 pos : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    // Linearize inputs
    float4 linColor = RingColor;
    linColor.rgb = pow(linColor.rgb, 2.2);

    float2 centered = uv - 0.5;
    float dist = length(centered);
    
    // Create a morphing effect
    // We add a sine wave perturbation to the radius check based on angle and time
    float angle = atan2(centered.y, centered.x);
    float morph = sin(angle * 5.0 + Time * 2.0) * 0.02 + cos(angle * 3.0 - Time) * 0.02;
    
    // Ring calculation
    // Target radius is 0.4 (so it fits in 0-1 uv space with room)
    float radius = 0.4 + morph;
    float halfThick = Thickness * 0.5;
    
    // Smoothstep for anti-aliased edges
    float alpha = smoothstep(radius - halfThick - 0.01, radius - halfThick, dist) * 
                  (1.0 - smoothstep(radius + halfThick, radius + halfThick + 0.01, dist));
    
    float4 result = linColor;
    result.a *= alpha;
    
    // Premultiply
    result.rgb *= result.a;
    
    return result; // Vertex color (color input) is usually white but good to multiply if the sprite batch sends tints
}

technique BasicColor
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPixelShader();
	}
}
