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
float4 CenterColor;
float4 EdgeColor;

// ----------------------------------------------------------------------------
// Noise
// ----------------------------------------------------------------------------

float hash(float2 p) {
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float noise(float2 p) {
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(hash(i), hash(i + float2(1,0)), f.x),
                lerp(hash(i + float2(0,1)), hash(i + float2(1,1)), f.x), f.y);
}

float fbm(float2 p) {
    float v = 0.0;
    float amp = 0.5;
    for (int i = 0; i < 3; i++) {
        v += amp * noise(p);
        p *= 2.0;
        amp *= 0.5;
    }
    return v;
}

// ----------------------------------------------------------------------------
// Pixel Shader
// ----------------------------------------------------------------------------

float4 MainPixelShader(float4 pos : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    // Coords: (-0.5, -0.5) top-left to (0.5, 0.5) bottom-right
    float2 p = uv - 0.5;
    
    // 1. Droplet Shape Base
    // We want a shape that is wider at the bottom (y > 0) and narrower at top (y < 0).
    // Let's offset Y slightly down so the bulk is lower.
    float2 shapeUV = p;
    shapeUV.y -= 0.15; 
    
    // Tapering: if y is negative (top), multiply x by something > 1 to make 'dist' grow faster (narrower shape)
    // Or divide by something < 1.
    // Let's use a simple geometric distortion: 
    // width decreases as we go up (y decreases).
    // map y range [-0.5, 0.5] -> width factor
    float widthFactor = 1.0 - (shapeUV.y * 0.8); // Simple linear taper
    
    // Calculate basic distance field for a droplet (circle distorted)
    float dist = length(shapeUV);
    
    // 2. Noise & Turbulence
    // We want chunks breaking off at the top.
    // So noise influence should be massive at the top, small at bottom.
    
    // Animate noise
    float2 noiseP = p * 2.5;
    noiseP.y -= Time * 1.5; // Flow Up
    
    float n = fbm(noiseP);
    
    // Shaping the noise influence:
    // Strong at top (y < 0), Weak at bottom (y > 0)
    // -p.y goes from -0.5 (bottom) to 0.5 (top).
    // Let's re-range p.y to [0, 1] roughly for blending.
    // Top (-0.5) -> High influence. Bottom (0.5) -> Low influence.
    float erosionGradient = saturate(0.5 - p.y); // 0 at bottom, 1 at top.
    
    // Make erosion strictly stronger at top to break chunks
    // 'erosion' subtracts from the solid shape
    float erosion = n * (0.3 + 1.2 * erosionGradient * erosionGradient); 
    // Squared gradient to focus effect really high up
    
    // 3. Combine Shape and Noise
    // Field function defining the fire: 1.0 (center) -> 0.0 (edge)
    // Base radius ~0.25
    float radius = 0.25;
    float field = radius - dist - erosion * 0.4; 
    // Note: dist increases outwards, so (radius - dist) is +ve inside, -ve outside.
    // 'erosion' reduces the volume.
    
    // 4. Hard Edge Cutoff
    // threshold: we want > 0.
    // But we want a border.
    // Solid Body: field > 0.0
    // Border: field ranges from roughly 0.0 to -0.05 maybe?
    
    // Let's define the solid mask.
    // Using step for 100% hard jagged edges.
    float alpha = step(0.0, field);
    
    // 5. Coloring
    // We want the interior to be CenterColor.
    // We want the EDGE (just inside the cutoff) to be EdgeColor.
    // How close to the edge are we?
    // 'field' is the signed distance roughly.
    // 0.0 is the edge. 0.25 is center.
    // Let's map [0.0, 0.1] to Edge->Center transition.
    
    // Sharp transition for the edge color too? "Strong reds defined by strong reds"
    // Let's make a band.
    float edgeBand = smoothstep(0.0, 0.08, field); // 0.0->Red, 0.08->Center
    
    // If alpha is 0, we discard anyway.
    
    float3 finalColor = lerp(EdgeColor.rgb, CenterColor.rgb, edgeBand);
    
    // 6. Final Output
    float4 result = float4(finalColor, alpha);
    
    // PREMULTIPLY (AlphaBlend requires this)
    // Since alpha is 0 or 1, this just masks it.
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
