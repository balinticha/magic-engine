using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace MagicEngine.Engine.Base.Shaders.PostProcessing;

public class PostProcessingManager 
{
    public bool Enabled { get; set; } = true;
    public List<PostProcessStep> Effects { get; } = new();
    public bool IsDebugMenuOpen { get; set; } = false;

    /// <summary>
    /// Applies a list of post-processing effects to the provided source texture and returns the processed texture.
    /// </summary>
    /// <param name="layer">The layer of when to perform the effect. <see cref="EffectType"/></param>
    /// <param name="sb">The <see cref="SpriteBatch"/> used for rendering operations during the post-processing effects.</param>
    /// <param name="source">The <see cref="Texture2D"/> source texture to which the effects are applied.</param>
    /// <param name="dest">The <see cref="RenderTarget2D"/> that will ultimately hold the processed result.</param>
    /// <param name="swap">A temporary <see cref="RenderTarget2D"/> used for intermediate effect application steps.</param>
    /// <returns>The <see cref="Texture2D"/> containing the final result after applying all enabled effects.</returns>
    public Texture2D ApplyEffects(EffectType layer, SpriteBatch sb, Texture2D source, RenderTarget2D dest, RenderTarget2D swap)
    {
        if (Effects.Count == 0 || !Enabled)
            return source;

        Texture2D currentSource = source;
        RenderTarget2D currentDest = swap;
        RenderTarget2D nextDest = dest;
        
        foreach (var effect in Effects)
        {
            if (effect.Type != layer) continue;
            if (!effect.Enabled) continue;
            
            effect.Apply(sb, currentSource, currentDest);

            currentSource = currentDest;
            (currentDest, nextDest) = (nextDest, currentDest);
        }
        return currentSource; 
    }
}

/// <summary>
/// Enumerates the types of effects that can be applied during post-processing.
/// These types define how individual shader effects are processed in the rendering pipeline.
/// TexelLayer - applies on the low-res, texel-perfect pixel texture. Any changes here will result in "perfect" texels on the final image
/// PixelLayer - applies on the upsized image. Any changes here will apply for each pixel on the final screen, breaking the pixel art grid.
/// </summary>
public enum EffectType
{
    TexelLayer, // applied before upscaling
    PixelLayer  // applied after upscaling
}