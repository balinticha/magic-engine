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
float4 ColorHigh;      // Bright glow color
float4 ColorLow;       // Dim fill color
float GridScale;       // Number of hexes
float BorderWidth;     // Thickness of hex lines
float PulseSpeed;      // Speed of the glow pulse
float EdgeFade;        // Unused in this version, kept for compatibility
float MaxBrightness;   // Clamp for HDR brightness

// --- HELPERS ---
float2 math_mod(float2 x, float2 y) {
    return x - y * floor(x / y);
}

// --- PIXEL SHADER ---
float4 PixelShaderHexShieldHard(float4 position : SV_POSITION, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    // --- DEFAULTS ---
    // If parameters are 0/unset, fall back to defaults
    float3 colHiParam = (length(ColorHigh.rgb) < 0.01) ? float3(0.4, 0.8, 1.0) : ColorHigh.rgb; 
    float3 colLoParam = (length(ColorLow.rgb) < 0.01)  ? float3(0.0, 0.1, 0.3) : ColorLow.rgb; 

    // LINEARIZE INPUTS (sRGB -> Linear)
    // We assume the inputs from generic color pickers are sRGB.
    float3 colHi = pow(abs(colHiParam), 2.2);
    float3 colLo = pow(abs(colLoParam), 2.2);

    float scale    = (GridScale < 0.01)         ? 10.0 : GridScale;
    float width    = (BorderWidth < 0.01)       ? 0.025 : BorderWidth * 0.5;
    float speed    = (PulseSpeed < 0.01)        ? 2.0  : PulseSpeed;
    float maxB     = (MaxBrightness < 0.01)     ? 4.0  : MaxBrightness;
    
    // Center UV
    float2 uv = texCoord - 0.5;
    float2 gridUV = uv * scale;
    
    // --- GRID GENERATION ---
    float2 r = float2(1.0, 1.7320508);
    float2 h = r * 0.5;
    
    float2 a = math_mod(gridUV, r) - h;
    float2 b = math_mod(gridUV - h, r) - h;
    
    // Determine which grid we are in
    bool pickA = dot(a, a) < dot(b, b);
    float2 gv = pickA ? a : b;
    
    // Calculate Distance for Border
    float x = abs(gv.x);
    float y = abs(gv.y);
    float d = max(x, x * 0.5 + y * 0.866025);
    
    // Calculate ID (Hex Center)
    float2 center = gridUV - gv;
    
    // ARTIFACT FIX:
    // Round the center coordinate to avoid floating point noise causing the hash to stutter.
    float2 id = floor(center * 100.0 + 0.5) / 100.0;
    
    // --- VISUALS ---
    
    // Border
    float distToEdge = 0.5 - d;
    float borderAlpha = smoothstep(width, width - 0.02, distToEdge);
    
    // Fill
    float fillAlpha = smoothstep(0.5, 0.0, d) * 0.5;
    
    // --- ANIMATION / PULSE ---
    // Make individual hexes pulse based on their ID
    float rand = frac(sin(dot(id, float2(12.9898, 78.233))) * 43758.5453);
    float pulse = sin(Time * speed + rand * 6.28) * 0.5 + 0.5;
    
    // Combine
    float3 finalColor = lerp(colLo, colHi, borderAlpha + pulse * 0.5);
    
    // --- MASK ---
    // Calculate distance of the HEX CENTER from the shield center (0,0)
    float2 hexCenterUV = id / scale;
    float hexDist = length(hexCenterUV);
    
    // Hard cutoff: Ensure the WHOLE hex fits in the quad.
    // Hex radius in UV space is approx 0.6 / scale.
    // We subtract this margin so hexes near the edge don't get sliced by the quad bounds.
    float mask = step(hexDist, 0.5 - (0.6 / scale));
    
    float alpha = max(borderAlpha, fillAlpha * pulse * 0.8);
    alpha *= mask;
    
    // Boost brightness (this is where HDR comes from)
    finalColor *= (1.2 + borderAlpha * 0.8); 
    
    // Clamp HDR Brightness
    finalColor = min(finalColor, float3(maxB, maxB, maxB));

    // PREMULTIPLY ALPHA (Critical Requirement)
    // Output should be (RGB * Alpha, Alpha)
    return float4(finalColor * alpha, alpha) * color;
}

technique HexShieldHard
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL PixelShaderHexShieldHard();
	}
}
