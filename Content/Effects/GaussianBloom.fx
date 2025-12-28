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
float2 InverseResolution; // 1.0 / Width, 1.0 / Height
float Threshold;
float SoftKnee; 
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
    float brightness = max(color.r, max(color.g, color.b));
    float knee = SoftKnee * Threshold;
    float soft = brightness - Threshold + knee;
    soft = clamp(soft, 0, 2 * knee);
    soft = soft * soft / (4 * knee + 0.00001);
    
    float contribution = max(soft, brightness - Threshold);
    contribution /= max(brightness, 0.00001);
    
    return float4(color * contribution, 1.0);
}

// ----------------------------------------------------------------------------------
// Pixel Shaders
// ----------------------------------------------------------------------------------

// Pass 1: Extract Bright Areas
float4 PSExtract(VertexShaderOutput input) : COLOR0
{
    float4 color = tex2D(TextureSampler, input.TexCoord);
    return Prefilter(color.rgb);
}

// Weights for Sigma ~2.5 (9-tap)
// 0.0, 1.0, 2.0, 3.0, 4.0
// Normalized manually roughly
// Center: 0.20
// 1: 0.18
// 2: 0.12
// 3: 0.07
// 4: 0.03
// Total half = 0.4. Full = 0.2 + 0.8 = 1.0. 
// Let's use computed gaussian weights for Sigma=3, Kernel=9
static const float weights[5] = { 0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216 }; 

// Pass 2: Blur Horizontal
float4 PSBlurH(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TexCoord;
    float3 color = tex2D(TextureSampler, uv).rgb * weights[0];
    
    for(int i = 1; i < 5; i++)
    {
        float offset = float(i) * InverseResolution.x;
        color += tex2D(TextureSampler, uv + float2(offset, 0)).rgb * weights[i];
        color += tex2D(TextureSampler, uv - float2(offset, 0)).rgb * weights[i];
    }
    
    return float4(color, 1.0);
}

// Pass 3: Blur Vertical
float4 PSBlurV(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TexCoord;
    float3 color = tex2D(TextureSampler, uv).rgb * weights[0];
    
    for(int i = 1; i < 5; i++)
    {
        float offset = float(i) * InverseResolution.y;
        color += tex2D(TextureSampler, uv + float2(0, offset)).rgb * weights[i];
        color += tex2D(TextureSampler, uv - float2(0, offset)).rgb * weights[i];
    }
    
    return float4(color, 1.0);
}

// Pass 4: Composite (Optional, mostly done via Additive Blend in C#)
float4 PSComposite(VertexShaderOutput input) : COLOR0
{
     float4 color = tex2D(TextureSampler, input.TexCoord);
     color.rgb *= Intensity;
     return color;
}


technique Extract
{
    pass P0 { PixelShader = compile PS_SHADERMODEL PSExtract(); }
}

technique BlurHorizontal
{
    pass P0 { PixelShader = compile PS_SHADERMODEL PSBlurH(); }
}

technique BlurVertical
{
    pass P0 { PixelShader = compile PS_SHADERMODEL PSBlurV(); }
}

technique Composite
{
    pass P0 { PixelShader = compile PS_SHADERMODEL PSComposite(); }
}
