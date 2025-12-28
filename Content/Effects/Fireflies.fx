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
float Density; // Grid density
float Size;    // Firefly size
float Speed;   // Movement speed
float4 BaseColor; // r,g,b,a
float HDRBoost; // Max brightness multiplier
float FadeSpeed; // Speed of fading in/out

// Pseudo-random function
float2 random2(float2 p) {
    return frac(sin(float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3))))*43758.5453);
}

float4 MainPixelShader(float4 pos : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    // Grid Setup
    float2 st = uv * Density;
    float2 i_st = floor(st);
    float2 f_st = frac(st);

    float m_dist = 1.0;  // Minimum distance
    float opacity = 0.0;
    float intensity = 0.0;

    // Iterate through neighbor cells (though for simple dots, checking center usually suffices, 
    // but 3x3 neighbor check ensures no clipping at edges if they move across borders)
    // For simplicity and "gentle flying" that stays mostly in cell, we can try just the current cell center first.
    // However, to allow them to cross boundaries freely, 3x3 search is better.
    
    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            float2 neighbor = float2(float(x), float(y));
            float2 pointInCell = random2(i_st + neighbor);
            
            // Animation: Gentle flying
            // Random offset + Sine wave movement
            float2 offset = 0.5 + 0.4 * sin(Time * Speed + 6.2831 * pointInCell);
            
            // Vector from pixel to point
            float2 diff = neighbor + offset - f_st;
            
            // Distance
            float dist = length(diff);
            
            // Draw firefly
            // Smooth glow
            if (dist < Size) {
                // Fade logic per firefly
                float fadePhase = pointInCell.x * 10.0; // Random phase
                float fade = 0.5 + 0.5 * sin(Time * FadeSpeed + fadePhase);
                
                // HDR Fluctuation: When visible (fade > 0.5 ish), pulse brightness
                float hdrPhase = pointInCell.y * 20.0;
                float currentBoost = lerp(1.0, HDRBoost, 0.5 + 0.5 * sin(Time * 3.0 + hdrPhase));
                
                // Combine
                float glow = smoothstep(Size, Size * 0.2, dist); // Soft edge
                
                // Additive accumulation not strictly needed if we assume sparse fireflies, 
                // but nice if they overlap.
                // For simplicity in this loop, we just take the max or accumulate.
                // Let's use max to avoid blowing out.
                float currentOpacity = glow * fade;
                
                if(currentOpacity > opacity) {
                    opacity = currentOpacity;
                    intensity = currentBoost;
                }
            }
        }
    }

    // Final Color Calculation
    float4 resultColor = BaseColor;
    
    // Apply opacity and HDR intensity
    float alpha = opacity * BaseColor.a;
    float3 rgb = BaseColor.rgb * intensity;
    
    // Premultiply Alpha (CRITICAL RULE)
    // Result = (RGB * Intensity * Alpha, Alpha)
    
    return float4(rgb * alpha, alpha) * color;
}

technique Fireflies
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPixelShader();
	}
}
