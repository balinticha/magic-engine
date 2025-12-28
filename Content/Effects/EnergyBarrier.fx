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

// Material Parameters
float4 BarrierColor;
float4 VoidColor;
float VisibleAngle; // In degrees
float Thickness;
float EdgeWidth;
float GapWidth;
float Speed;

#define PI 3.14159265

float4 MainPixelShader(float4 pos : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    // 1. Linearize inputs
    float4 linearBarrierColor = BarrierColor;
    linearBarrierColor.rgb = pow(linearBarrierColor.rgb, 2.2);
    
    float4 linearVoidColor = VoidColor;
    linearVoidColor.rgb = pow(linearVoidColor.rgb, 2.2);

    // 2. Polar Coordinates
    float2 center = float2(0.5, 0.5);
    float2 dir = uv - center;
    float dist = length(dir);
    float angle = atan2(dir.y, dir.x); // -PI to PI
    
    // Normalize angle to 0-360 range for easier comparison
    float degAngle = degrees(angle);
    if (degAngle < 0) degAngle += 360.0;
    
    // 3. Ring Geometry Calculation
    float radius = 0.4;
    float halfThick = Thickness * 0.5;
    
    // Distance from center of ring
    float d = abs(dist - radius);
    
    // Outside the total thickness?
    // AA Softness
    float aa = 0.005;
    float totalMask = smoothstep(halfThick + aa, halfThick, d);
    
    // Zones:
    // Core is in center (d close to 0)
    // Gap comes next
    // Edge comes last (d close to halfThick)
    
    // We can define boundaries based on widths
    // Core Half Width = halfThick - EdgeWidth - GapWidth
    // Let's protect against negative values
    float coreHalfWidth = max(0.0, halfThick - EdgeWidth - GapWidth);
    float gapEnd = coreHalfWidth + GapWidth;
    
    // Determine which zone we are in
    // Core: 0 to coreHalfWidth
    // Gap: coreHalfWidth to gapEnd
    // Edge: gapEnd to halfThick
    
    // Masks
    // Is Core?
    float isCore = smoothstep(coreHalfWidth, coreHalfWidth - aa, d);
    
    // Is Edge?
    // Edge starts at gapEnd
    float isEdge = smoothstep(gapEnd - aa, gapEnd, d);
    
    // Is Gap? (Not Core and Not Edge) -> Implicit transparent
    
    // 4. Angle Mask
    // Assuming 0 degrees is Right.
    float targetAngleRad = 0.0; // Facing Right
    float angleDiff = abs(angle - targetAngleRad);
    if (angleDiff > PI) angleDiff = 2.0 * PI - angleDiff;
    
    float visibleHalfAngleRad = radians(VisibleAngle) * 0.5;
    float angleMask = smoothstep(visibleHalfAngleRad + 0.05, visibleHalfAngleRad, angleDiff); 
    
    // 5. Visuals
    
    // Cosmic Void Effect for Core
    float timeScale = Time * Speed;
    float noise1 = sin(angle * 10.0 + timeScale + dist * 20.0);
    float noise2 = cos(angle * 15.0 - timeScale * 0.5 + dist * 10.0);
    float voidPattern = (noise1 + noise2) * 0.5 + 0.5; // 0 to 1
    
    float4 coreColor = lerp(linearVoidColor, linearBarrierColor, voidPattern * 0.5);
    
    // Mix Color
    // If Edge -> BarrierColor
    // If Core -> coreColor
    // If Gap -> Transparent (0,0,0,0)
    
    float4 finalColor = float4(0,0,0,0);
    
    // Add Core
    finalColor += coreColor * isCore;
    
    // Add Edge
    finalColor += linearBarrierColor * isEdge;
    
    // Apply masks
    // totalMask ensures we clip strictly at the outer bounds
    // angleMask handles the arc
    float alpha = totalMask * angleMask;
    
    // Premultiply
    return finalColor * alpha;
}

technique EnergyBarrier
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPixelShader();
	}
}
