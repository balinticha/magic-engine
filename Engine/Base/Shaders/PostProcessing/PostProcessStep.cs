using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MagicEngine.Engine.Base.Shaders.PostProcessing;

public abstract class PostProcessStep
{
    public bool Enabled { get; set; } = true;
    // public set so the debug tool can change this
    public EffectType Type { get; set; }
    public string Name { get; set; }

    /// <summary>
    /// Applies the post-processing effect to the provided source texture and renders the result
    /// into the specified destination render target.
    /// </summary>
    /// <param name="sb">The <see cref="SpriteBatch"/> used for rendering operations.</param>
    /// <param name="source">The <see cref="Texture2D"/> source texture to which the effect is applied.</param>
    /// <param name="destination">The <see cref="RenderTarget2D"/> that serves as the target for the processed texture.</param>
    public abstract void Apply(SpriteBatch sb, Texture2D source, RenderTarget2D destination);
    
    protected void SetStandardParameters(Effect effect, RenderTarget2D currentDestination)
    {
        var sizeParameter = effect.Parameters["ScreenSize"];
        if (sizeParameter != null)
        {
            sizeParameter.SetValue(new Vector2(currentDestination.Width, currentDestination.Height));
        }
    }
}