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
float LaserSpeed;
float PulseIntensity;
float Wobble;
float HDRBoost;
float4 Color;
float CoreHotness;

float4 MainPixelShader(float4 pos : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    // Linearize the input color
    float3 linearizedColor = pow(Color.rgb, 2.2);
    
    // Wobble effect
    float wobbleOffset = sin(uv.y * 10.0 + Time * 5.0) * Wobble;
    float centeredX = abs(uv.x - 0.5 + wobbleOffset) * 2.0; // 0 at center, 1 at edges
    
    // Core beam shape (exponential falloff)
    float beamIntensity = pow(1.0 - centeredX, CoreHotness);
    
    // Pulse effect
    // Move pulses up or down based on LaserSpeed
    float pulse = sin(uv.y * 20.0 - Time * LaserSpeed * 10.0); 
    // Remap sine to [1.0, 1.0 + PulseIntensity] range roughly, or [1.0 - PulseIntensity, 1.0 + PulseIntensity]
    // Let's make it additive: base 1.0 + pulse
    float pulseFactor = 1.0 + pulse * PulseIntensity;
    
    // Combine everything
    float3 finalRGB = linearizedColor * beamIntensity * pulseFactor * HDRBoost;
    
    // Calculate Alpha
    // We want the edges to be transparent.
    float alpha = beamIntensity; 
    
    // Premultiply
    // Verify alpha isn't too low to avoid full invisibility if user expects a solid beam, 
    // but beamIntensity falls to 0 at edges, which is correct.
    // Ensure alpha is clamped.
    alpha = saturate(alpha);
    
    float4 result = float4(finalRGB * alpha, alpha);
    
    return result * color; 
}

technique BasicColor
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPixelShader();
	}
}
