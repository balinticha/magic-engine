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
float4 TargetColor;

float4 MainPixelShader(float4 pos : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    // Start with the target parameter color
    float4 result = TargetColor;
    result.rgb = pow(result.rgb, 2.2);
    
    // Premultiply Alpha
    result.rgb *= result.a;
    
    // Multiply by vertex color (for engine fading/tinting support)
    return result * color;
}

technique BasicColor
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPixelShader();
	}
}
