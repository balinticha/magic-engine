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
float Density;      // Radial density mainly
float Size;         // Spark size
float Speed;        // Outward speed
float4 BaseColor;   // r,g,b,a
float HDRBoost;     // Max brightness at center
float FadeSpeed;    // Not used for radial fade, but maybe for individual sparkle flicker?

// Constants
static const float PI = 3.14159265359;
static const float TWO_PI = 6.28318530718;

// Pseudo-random function
float2 random2(float2 p) {
    p = float2(dot(p,float2(127.1,311.7)), dot(p,float2(269.5,183.3)));
    return frac(sin(p)*43758.5453);
}

float4 MainPixelShader(float4 pos : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    // Center UVs
    float2 st = uv - 0.5;
    
    // Convert to Polar
    float r = length(st) * 2.0; // 0 to 1 (at edges approx)
    float a = atan2(st.y, st.x) / TWO_PI + 0.5; // 0 to 1
    
    // Polar Grid
    // X axis = Radius (moving), Y axis = Angle (cyclic)
    float radialVal = r * Density - Time * Speed;
    float angularVal = a * Density * 3.0; // Higher density angularly looks better usually
    
    float2 gridPosition = float2(radialVal, angularVal);
    float2 gridID = floor(gridPosition);
    float2 gridF = frac(gridPosition);
    
    float m_dist = 1.0;
    float totalGlow = 0.0;
    float maxBoost = 0.0;
    
    // 3x3 Grid Search
    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            float2 neighbor = float2(float(x), float(y));
            
            // Handle Angular Wrapping for ID
            // We need to check if the neighbor crosses the 0/1 boundary in 'a'
            // However, since 'a' is continuous 0..1, 'angularVal' is 0..Density*3
            // True wrapping requires 'gridID.y' to wrap modulo (Density*3).
            // But Density might not be integer.
            // visual seam is acceptable for now given "Explosion" chaos, 
            // but let's try to just map unique IDs.
            
            float2 id = gridID + neighbor;
            
            // Random position in cell
            float2 pointInCell = random2(id);
            
            // Jitter position
            float2 offset = 0.5 + 0.4 * sin(Time * 2.0 + 6.2831 * pointInCell);
            
            // Calculate distance in POLAR space?
            // No, particles look distorted if we render circles in polar space (they become arcs).
            // We want round sparks.
            
            // We need to reconstruct the Cartesian position of the spark relative to the current pixel.
            
            // 1. Spark's Polar Coord
            float sparkR_grid = id.x + offset.x; // this is in grid units
            float sparkA_grid = id.y + offset.y;
            
            // Convert back to normalized polar (undo grid scaling)
            // Note: We effectively added 'Time * Speed' to gridID.x during the floor(), 
            // so we need to match that frame of reference or reconstruct absolute R.
            
            // grid_r was r * Density - Time * Speed
            // so r ~ (grid_r + Time * Speed) / Density
            float sparkR = (sparkR_grid + Time * Speed) / Density;
            float sparkA = sparkA_grid / (Density * 3.0);
            
            // Convert to Cartesian
            float sparkA_rad = (sparkA - 0.5) * TWO_PI;
            float2 sparkPos = float2(cos(sparkA_rad), sin(sparkA_rad)) * (sparkR * 0.5); 
            // *0.5 because r calculated earlier was length(st)*2
            
            // Actual distance in screen space (st is -0.5 to 0.5 range)
            float dist = length(st - sparkPos);
            
            // Check visibility
            // Scale size by radius? Maybe smaller as they go out?
            float sizeMod = Size * (1.0 - sparkR * 0.5); 
            if (sizeMod < 0) sizeMod = 0;

            if (dist < sizeMod) {
                // Spark Logic
                float fade = 1.0 - smoothstep(0.0, 1.0, sparkR); // Fade out as it goes out
                
                // Add flicker
                float flicker = 0.5 + 0.5 * sin(Time * 10.0 + pointInCell.x * 100.0);
                
                float glow = smoothstep(sizeMod, sizeMod * 0.1, dist);
                
                float intensity = lerp(1.0, HDRBoost, (1.0 - sparkR)); // High HDR at center
                if (intensity < 1.0) intensity = 1.0;
                
                totalGlow += glow * fade * flicker;
                maxBoost = max(maxBoost, intensity);
            }
        }
    }
    
    // Safety for additive accumulation
    if (totalGlow > 1.0) totalGlow = 1.0;
    
    float4 resultColor = BaseColor;
    float alpha = totalGlow * BaseColor.a;
    float3 rgb = BaseColor.rgb * maxBoost;
    
    // Premultiply
    return float4(rgb * alpha, alpha) * color;
}

technique Spark
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPixelShader();
	}
}
