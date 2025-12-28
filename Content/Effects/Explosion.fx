#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// --- PARAMETERS ---
sampler TextureSampler : register(s0);
float Time;

// Exposed Parameters
float3 ColorHot;       // Initial explosion color
float3 ColorCool;      // Faded color
float LoopDuration;    // TOTAL time between explosions
float MaxRadius;       // Max expansion size (0.0 to 1.0 UV space)
float ShockwaveHDRBoost; // Boost intensity for the shockwave
float ExplosionDuration; // Time for the explosion animation within the loop
float ShockwaveCoolDown; // How fast the shockwave fades out

// --- HELPERS ---
float hash(float2 p) {
    p = frac(p * float2(234.34, 987.21));
    p += dot(p, p + 87.32);
    return frac(p.x * p.y);
}

// Value Noise (Simple, based on hash)
float noise(float2 p) {
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    
    float res = lerp(lerp(hash(i), hash(i + float2(1.0, 0.0)), f.x),
                     lerp(hash(i + float2(0.0, 1.0)), hash(i + float2(1.0, 1.0)), f.x), f.y);
    return res;
}

// Particle Helper
float GetParticle(float2 uv, float t, float radius, float maxSpeed) {
    // V = P / t
    float2 v_req = uv / t;
    
    // Fix: "Contracting" artifacts.
    if (length(v_req) > maxSpeed) return 0.0;

    // Grid density for velocities
    float density = 20.0; 
    float2 v_cell = floor(v_req * density);
    
    // Random check: does this velocity cell spawn a particle?
    float h = hash(v_cell);
    if (h > 0.97) { // 3% density
        // Center of this velocity cell
        float2 v_center = (v_cell + 0.5) / density;
        
        // Add random offset
        float2 v_offset = (float2(hash(v_cell * 1.2), hash(v_cell * 3.4)) - 0.5) / density * 0.8;
        float2 v_final = v_center + v_offset;
        
        // Where is this particle now?
        float2 pos = v_final * t;
        
        // Distance to current pixel
        float dist = length(uv - pos);
        
        // Draw particle
        float size = radius * (1.0 - t * 0.5); 
        return 1.0 - smoothstep(0.005 * size, 0.02 * size, dist);
    }
    return 0.0;
}

// --- PIXEL SHADER ---
float4 PixelShaderExplosion(float4 position : SV_POSITION, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    // Linearize Inputs (Rule from shader_writing.md 1.3)
    float3 linearColorHot = pow(ColorHot, 2.2);
    float3 linearColorCool = pow(ColorCool, 2.2);

    // Defaults
    float3 cHot    = (length(linearColorHot) < 0.001)   ? float3(1.0, 0.45, 0.04) : linearColorHot; // Adjusted approximate linear default
    float3 cCool   = (length(linearColorCool) < 0.001)  ? float3(1.0, 0.04, 0.002) : linearColorCool; // Adjusted approximate linear default
    float loopTime = (LoopDuration < 0.01)       ? 2.0 : LoopDuration;
    float radiusMax= (MaxRadius < 0.01)          ? 0.45 : MaxRadius;
    float hdrBoost = (ShockwaveHDRBoost < 0.01)  ? 1.0 : ShockwaveHDRBoost;
    float animDuration = (ExplosionDuration < 0.01) ? 1.3 : ExplosionDuration;
    float coolDown = (ShockwaveCoolDown < 0.01) ? 3.0 : ShockwaveCoolDown;

    float2 uv = texCoord - 0.5;
    float dist = length(uv);
    
    // Time Logic
    float t_cycle = frac(Time / loopTime);
    float t_seconds = t_cycle * loopTime;
    
    // Normalized animation time
    float t = t_seconds / animDuration;
    
    // Optimization: Early discard
    if (t > 1.0) discard; 
    
    // --- 1. SHOCKWAVE (Additive) ---
    float waveRadius = t * radiusMax * 1.5;
    float waveWidth = 0.03;
    float shockwave = smoothstep(waveWidth, 0.0, abs(dist - waveRadius));
    float waveAlpha = exp(-t * coolDown);
    float3 waveColor = float3(1.0, 0.8, 0.6) * shockwave * waveAlpha * 1.5 * hdrBoost; // Slightly tweaked for linear

    // --- 2. PARTICLES (Additive) ---
    // Max Physical Speed = Distance / Time. 
    float maxParticleSpeed = (radiusMax / animDuration) * 3.0; 

    // Pass maxSpeed to fix contracting artifact
    float spark = GetParticle(uv, t_seconds * 0.6, 1.0, maxParticleSpeed); 
    
    // Fade in at start to hide "pop/contraction" artifacts
    float startFade = smoothstep(0.0, 0.1, t);
    
    // Fade out Logic
    float distFade = 1.0 - smoothstep(radiusMax * 0.8, radiusMax * 0.95, dist);
    float timeFade = smoothstep(1.0, 0.7, t); 
    
    float3 partColor = lerp(cHot, cCool, t); 
    float3 particleLight = spark * partColor * 2.5 * timeFade * distFade * startFade;

    // --- 3. FLASH (Additive) ---
    float n = noise(normalize(uv) * 10.0 + t_seconds * 5.0); 
    float flashDist = dist - n * 0.15; 
    
    float flash = exp(-t_seconds * 15.0) * 1.5; 
    float3 flashColor = float3(1.0, 1.0, 0.8) * flash * (1.0 - smoothstep(0.0, 0.4, flashDist));

    // --- 4. COMPOSITION ---
    float3 finalRGB = waveColor + particleLight + flashColor;
    float finalAlpha = 0.0; // Additive effects usually don't have alpha, but we can set it if we want some masking
    
    // Core glow 
    float coreGlow = exp(-dist * 4.0 - t * 4.0) * 0.3;
    finalRGB += cHot * coreGlow;
    
    // Global Edge Fade
    float edgeFade = 1.0 - smoothstep(radiusMax * 0.9, radiusMax, dist);
    finalRGB *= edgeFade;

    // Output Premultiplied Linear
    // Since this is largely additive light, we interpret it as having 0 alpha for the "solid" part 
    // BUT to blend additively in AlphaBlend mode (One, InvSrcAlpha), we rely on alpha to be 0 so the Dest isn't darkened.
    // However, if we want strict Additive behavior within AlphaBlend, we output (RGB, 0).
    // If we want it to hold its own weight, we need alpha.
    // The visual looks like an explosion, so mostly Light.
    
    // Ensure we don't return negative values
    finalRGB = max(finalRGB, 0.0);
    
    return float4(finalRGB, finalAlpha) * color; 
}

technique Explosion
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL PixelShaderExplosion();
	}
}
