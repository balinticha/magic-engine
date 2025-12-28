using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MagicEngine.Engine.Base.Shaders.PostProcessing.Implementations
{
    public class AcesToneMapper : PostProcessStep
    {
        public float Exposure { get; set; } = .79f;
        
        /// <summary>
        /// Strength of the Interleaved Gradient Noise dithering.
        /// Typical values are around 1.0/255.0 (~0.004) for 8-bit output.
        /// Set to 0 to disable.
        /// </summary>
        public float IgnDitherStrength { get; set; } = 1f / 255f; 
        
        private Effect _effect;

        public AcesToneMapper(Effect effect)
        {
            _effect = effect;
            Type = EffectType.PixelLayer; // Runs on the final high-res buffer
        }

        public override void Apply(SpriteBatch sb, Texture2D source, RenderTarget2D destination)
        {
            // Set Parameters
            _effect.Parameters["Exposure"]?.SetValue(Exposure);
            _effect.Parameters["IgnDitherStrength"]?.SetValue(IgnDitherStrength);
            _effect.Parameters["ScreenSize"]?.SetValue(new Vector2(destination.Width, destination.Height));


            // Draw
            sb.GraphicsDevice.SetRenderTarget(destination);
            
            // Standard Opaque draw with the shader
            sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, _effect);
            sb.Draw(source, new Rectangle(0, 0, destination.Width, destination.Height), Color.White);
            sb.End();
        }
    }
}
