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
float2 InverseResolution; // 1.0 / TextureWidth, 1.0 / TextureHeight
float Threshold;
float SoftKnee; // Usually 0.5 or closely related
float Intensity;

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

// ----------------------------------------------------------------------------------
// Helper Functions
// ----------------------------------------------------------------------------------

// Prefilter: Threshold with Soft Knee
float4 Prefilter(float3 color)
{
    // Standard luma (or max component)
    float brightness = max(color.r, max(color.g, color.b));
    
    // Soft threshold
    float knee = SoftKnee * Threshold; // Or just passed in directly as 'knee'
    float soft = brightness - Threshold + knee;
    soft = clamp(soft, 0, 2 * knee);
    soft = soft * soft / (4 * knee + 0.00001);
    
    float contribution = max(soft, brightness - Threshold);
    contribution /= max(brightness, 0.00001); // Render doc says minimize singularity
    
    return float4(color * contribution, 1.0); // 1.0 alpha? 
    // Wait, Premultiplied Alpha rule: Output is (RGB * Alpha, Alpha).
    // The bloom buffer itself is usually opaque, or additive.
    // Let's assume transparency isn't main factor here, but brightness is.
}


// ----------------------------------------------------------------------------------
// Pixel Shaders
// ----------------------------------------------------------------------------------

// Pass 1: Extract Bright Areas + Downsample (with Karis Average)
// We sample 5 positions (Center + 4 corners) or just 4 for box?
// Karis suggests a box filter on the first downsample.
float4 PSExtractAndDownsample(VertexShaderOutput input) : COLOR0
{
    // Box filter locations
    float2 uv = input.TexCoord;
    float2 offset = InverseResolution; // Source texel size
    
    // Sample 4 taps (Box)
    float3 c1 = tex2D(TextureSampler, uv + float2(-offset.x, -offset.y)).rgb;
    float3 c2 = tex2D(TextureSampler, uv + float2( offset.x, -offset.y)).rgb;
    float3 c3 = tex2D(TextureSampler, uv + float2(-offset.x,  offset.y)).rgb;
    float3 c4 = tex2D(TextureSampler, uv + float2( offset.x,  offset.y)).rgb;
    
    // We don't use karis average because it does not blend well with the
    // target pixel art style
    float3 avg = (c1 + c2 + c3 + c4) * 0.25;
    
    // Apply Threshold
    return Prefilter(avg);
}

// Pass 2: Downsample (Standard 13-tap)
// Based on "Dual Filtering" standard
float4 PSDownsample(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TexCoord;
    float2 x = float2(InverseResolution.x, 0);
    float2 y = float2(0, InverseResolution.y);
    
    float offsetScale = 1.0;
    
    // We want the total weight to be 1.0.
    // Group 1: Center (Weight 0.5) - Samples the immediate 2x2 area
    float3 center = tex2D(TextureSampler, uv).rgb;
    
    // Group 2: The 4 Corners (Weight 0.125 each -> Total 0.5)
    // We sample halfway between the center and the edge to grab the surrounding pixels
    float3 c1 = tex2D(TextureSampler, uv - offsetScale*x - offsetScale*y).rgb;
    float3 c2 = tex2D(TextureSampler, uv + offsetScale*x - offsetScale*y).rgb;
    float3 c3 = tex2D(TextureSampler, uv - offsetScale*x + offsetScale*y).rgb;
    float3 c4 = tex2D(TextureSampler, uv + offsetScale*x + offsetScale*y).rgb;
    
    // Math: 0.5 + (4 * 0.125) = 1.0
    float3 color = center * 0.5 + (c1 + c2 + c3 + c4) * 0.125;
    
    return float4(color, 1.0);
}

// Pass 3: Upsample (3x3 Tent Filter)
float4 PSUpsample(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TexCoord;
    
    // Radius should be adaptable but fixed usually fine
    float sampleScale = 1.0; 
    float2 d = InverseResolution * sampleScale;
    
    // 9-tap Tent Filter
    float3 s1 = tex2D(TextureSampler, uv - d).rgb;
    float3 s2 = tex2D(TextureSampler, uv + float2(0, -d.y)).rgb;
    float3 s3 = tex2D(TextureSampler, uv + float2(d.x, -d.y)).rgb;
    float3 s4 = tex2D(TextureSampler, uv + float2(-d.x, 0)).rgb;
    float3 s5 = tex2D(TextureSampler, uv).rgb;
    float3 s6 = tex2D(TextureSampler, uv + float2(d.x, 0)).rgb;
    float3 s7 = tex2D(TextureSampler, uv + float2(-d.x, d.y)).rgb;
    float3 s8 = tex2D(TextureSampler, uv + float2(0, d.y)).rgb;
    float3 s9 = tex2D(TextureSampler, uv + d).rgb;
    
    // 1 2 1
    // 2 4 2
    // 1 2 1
    // Total 16
    
    float3 color = (s1 + s3 + s7 + s9) * 1.0;
    color += (s2 + s4 + s6 + s8) * 2.0;
    color += s5 * 4.0;
    color *= (1.0 / 16.0);
   
    
    return float4(color * Intensity, 1.0);
}

technique ExtractAndDownsample
{
    pass P0 { PixelShader = compile PS_SHADERMODEL PSExtractAndDownsample(); }
}

technique Downsample
{
    pass P0 { PixelShader = compile PS_SHADERMODEL PSDownsample(); }
}

technique Upsample
{
    pass P0 { PixelShader = compile PS_SHADERMODEL PSUpsample(); }
}

