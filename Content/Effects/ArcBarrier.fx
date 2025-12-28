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
float3 ColorCore;      // Inner glowing color (Cyan)
float3 ColorEdge;      // Outer rim color (Deep Blue)
float Angle;           // The total angle of the arc in degrees
float Radius;          // Radius of the arc (UV space)
float Thickness;       // Thickness of the barrier
float Softness;        // Edge fade softness
float Speed;           // Animation speed

// --- HELPERS ---
float hash(float2 p) {
    p = frac(p * float2(234.34, 987.21));
    p += dot(p, p + 87.32);
    return frac(p.x * p.y);
}

float noise(float2 p) {
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(hash(i), hash(i + float2(1.0, 0.0)), f.x),
                lerp(hash(i + float2(0.0, 1.0)), hash(i + float2(1.0, 1.0)), f.x), f.y);
}

// FBM for roiling plasma
float fbm(float2 p) {
    float v = 0.0;
    float a = 0.5;
    float2 shift = float2(100.0, 100.0);
    // Rotate to reduce axial bias
    float2x2 rot = float2x2(cos(0.5), sin(0.5), -sin(0.5), cos(0.5));
    for (int i = 0; i < 3; ++i) {
        v += a * noise(p);
        p = mul(p, rot) * 2.0 + shift;
        a *= 0.5;
    }
    return v;
}

// Procedural Spark Generation (Grid based)
// Checks if a spark exists at uv at time t
float sparks(float2 uv, float t) {
    float density = 15.0; // Grid cells
    float2 id = floor(uv * density);
    float2 local = frac(uv * density) - 0.5;
    
    float h = hash(id);
    if (h > 0.95) { // 5% of cells have a spark
        // Animate position within cell
        float sparkLife = frac(t * 2.0 + h * 10.0); // 0 to 1 loop
        float size = sin(sparkLife * 3.14) * 0.4;
        
        // Random drift
        float2 offset = float2(
            sin(t * 5.0 + h * 100.0) * 0.3, 
            cos(t * 4.0 + h * 50.0) * 0.3
        );
        
        float d = length(local - offset);
        return smoothstep(size, size * 0.5, d);
    }
    return 0.0;
}

// --- PIXEL SHADER ---
float4 PixelShaderArcBarrier(float4 position : SV_POSITION, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    // --- DEFAULTS ---
    // Handle uninitialized parameters with reasonable defaults
    float3 cCore  = (length(ColorCore) < 0.01) ? float3(0.5, 0.9, 1.0) : ColorCore;
    float3 cEdge  = (length(ColorEdge) < 0.01) ? float3(0.0, 0.2, 0.8) : ColorEdge;
    float angleDeg= (Angle < 1.0)              ? 120.0 : Angle;
    float radius  = (Radius < 0.01)            ? 0.4   : Radius;
    float thick   = (Thickness < 0.001)        ? 0.05  : Thickness;
    float soft    = (Softness < 0.001)         ? 0.02  : Softness;
    float speed   = (Speed < 0.001)            ? 1.0   : Speed;

    // --- LINEARIZE COLORS (CRITICAL) ---
    // Assuming inputs from C# are sRGB, we must linearize them here.
    cCore = pow(cCore, 2.2);
    cEdge = pow(cEdge, 2.2);

    // UV center is (0.5, 0.5)
    float2 uv = texCoord - 0.5;
    
    // Polar Conversion
    float dist = length(uv);
    float angle = degrees(atan2(uv.y, uv.x)); 

    // ----------------------------------------------------------------------
    // 1. ARC MASK
    // ----------------------------------------------------------------------
    float halfAngle = angleDeg * 0.5;
    float absAngle = abs(angle);
    float angleDiff = halfAngle - absAngle;
    
    float angleFade = smoothstep(-soft * 100.0, soft * 100.0, angleDiff);
    
    // ----------------------------------------------------------------------
    // 2. RING SHAPE & RIM GLOW
    // ----------------------------------------------------------------------
    float d = abs(dist - radius);
    float ringBody = 1.0 - smoothstep(thick * 0.5 - soft, thick * 0.5, d);
    
    float rimPos = d / (thick * 0.5); 
    float rim = smoothstep(0.6, 0.95, rimPos); 
    
    // ----------------------------------------------------------------------
    // 3. ENERGY FIELD
    // ----------------------------------------------------------------------
    float t = Time * speed;
    
    // Layer 1
    float n1 = noise(float2(angle * 0.05, dist * 2.0 - t * 0.5));
    // Layer 2
    float n2 = fbm(float2(angle * 0.1 + t, uv.x * 5.0 + uv.y * 5.0)); 
    // Interference
    float interference = sin(dist * 400.0 + t * 5.0) * 0.5 + 0.5;
    // Scanlines (Vertical sweeping lines)
    float scan = sin(uv.x * 20.0 - t * 5.0) * sin(uv.y * 10.0 + t) * 0.5 + 0.5;
    float scanBright = smoothstep(0.95, 1.0, scan);

    float energy = n1 * 0.5 + n2 * 0.5;
    
    // ----------------------------------------------------------------------
    // 4. PARTICLES
    // ----------------------------------------------------------------------
    // We want particles drifting OUT from the shield surface
    // Just map UV to a scrolling field slightly
    float sparkVal = sparks(uv * 2.0 + float2(t * 0.1, 0.0), t);
    // Mask sparks to be generally near the arc but wider
    float sparkMask = (1.0 - smoothstep(thick * 0.5, thick * 2.0, d)) * angleFade; 
    
    // ----------------------------------------------------------------------
    // 5. COLOR GRADING
    // ----------------------------------------------------------------------
    float3 finalColor = lerp(cEdge, cCore, energy);
    
    // Hot spots
    float hotSpot = smoothstep(0.7, 1.0, n2 * n1);
    finalColor += float3(1.0, 1.0, 1.0) * hotSpot * 2.0;
    
    // Rim Glow
    finalColor += cCore * rim * 2.0;
    
    // Interference
    finalColor += cCore * interference * 0.1 * angleFade; 
    
    // Scanlines
    finalColor += float3(1.0, 1.0, 1.0) * scanBright * 0.5 * angleFade * ringBody;
    
    // Particles
    finalColor += float3(1.0, 0.9, 0.5) * sparkVal * sparkMask * 3.0; // Amber sparks
    cCore = pow(cCore, 2.2); // Just to match amber sparks if we wanted, but let's stick to hardcoded for now or use a param.
    // Actually the hardcoded amber above is linear-ish (1.0, 0.9, 0.5) is bright.
    
    // ----------------------------------------------------------------------
    // 6. ALPHA & OUTPUT
    // ----------------------------------------------------------------------
    // Combine masks
    float mask = max(ringBody, sparkVal * sparkMask) * angleFade;
    
    if (mask < 0.001) discard;
    
    // Overdrive (Overall Boost)
    finalColor *= 1.2;
    
    float alpha = mask;
    
    // --- PREMULTIPLY (CRITICAL) ---
    // Multiply RGB by Alpha for correct blending in AlphaBlend or Additive modes
    return float4(finalColor * alpha, alpha) * color;
}

technique ArcBarrier
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL PixelShaderArcBarrier();
	}
}
