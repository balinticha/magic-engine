using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MagicThing.Engine.Base.Shaders.PostProcessing.Implementations
{
    public class GaussianBloom : PostProcessStep
    {
        // Customizable Parameters
        public float Threshold { get; set; } = 1.6f;
        public float SoftKnee { get; set; } = 0.5f;
        public float Intensity { get; set; } = .5f;
        public float Sigma { get; set; } = 3.0f; // Controls blur width (not used directly in shader atm as we hardcoded weights, but kept for future calc)

        // Internal State
        private RenderTarget2D _ping;
        private RenderTarget2D _pong;
        private int _lastWidth;
        private int _lastHeight;
        
        private Effect _effect;

        public GaussianBloom(Effect effect)
        {
            _effect = effect;
            Type = EffectType.PixelLayer;
        }

        public override void Apply(SpriteBatch sb, Texture2D source, RenderTarget2D destination)
        {
            var device = sb.GraphicsDevice;
            int width = destination.Width;
            int height = destination.Height;

            // 1. Manage RenderTargets
            if (_ping == null || width != _lastWidth || height != _lastHeight)
            {
                DisposeTargets();
                InitializeTargets(device, width, height);
                _lastWidth = width;
                _lastHeight = height;
            }

            // ---------------------------------------------------------------------
            // Pass 1: Extract (Source -> Ping)
            // ---------------------------------------------------------------------
            _effect.CurrentTechnique = _effect.Techniques["Extract"];
            _effect.Parameters["Threshold"]?.SetValue(Threshold);
            _effect.Parameters["SoftKnee"]?.SetValue(SoftKnee);
            
            DrawPass(sb, source, _ping, _effect);

            // ---------------------------------------------------------------------
            // Pass 2: Blur Horizontal (Ping -> Pong)
            // ---------------------------------------------------------------------
            _effect.CurrentTechnique = _effect.Techniques["BlurHorizontal"];
            DrawPass(sb, _ping, _pong, _effect); // Use Ping as input (it has extracted brights)

            // ---------------------------------------------------------------------
            // Pass 3: Blur Vertical (Pong -> Ping)
            // ---------------------------------------------------------------------
            _effect.CurrentTechnique = _effect.Techniques["BlurVertical"];
            DrawPass(sb, _pong, _ping, _effect);
            
            _effect.CurrentTechnique = _effect.Techniques["BlurHorizontal"];
            DrawPass(sb, _ping, _pong, _effect);

            // Blur Vertical (Pong -> Ping)
            _effect.CurrentTechnique = _effect.Techniques["BlurVertical"];
            DrawPass(sb, _pong, _ping, _effect);
            

            // Now _ping has the fully blurred bloom

            // ---------------------------------------------------------------------
            // Final Combine
            // ---------------------------------------------------------------------
            
            device.SetRenderTarget(destination);
            
            // 1. Draw Original Source (Opaque)
            sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            sb.Draw(source, new Rectangle(0, 0, width, height), Color.White);
            sb.End();
            
            // 2. Draw Bloom (Additive)
            _effect.CurrentTechnique = _effect.Techniques["Composite"];
            _effect.Parameters["Intensity"]?.SetValue(Intensity);
            
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, _effect);
            sb.Draw(_ping, new Rectangle(0, 0, width, height), Color.White);
            sb.End();
        }

        private void DrawPass(SpriteBatch sb, Texture2D input, RenderTarget2D output, Effect effect)
        {
            var device = sb.GraphicsDevice;
            device.SetRenderTarget(output);
            
            Vector2 invRes = new Vector2(1.0f / input.Width, 1.0f / input.Height);
            effect.Parameters["InverseResolution"]?.SetValue(invRes);
            
            sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, effect);
            sb.Draw(input, new Rectangle(0, 0, output.Width, output.Height), Color.White);
            sb.End();
        }

        private void InitializeTargets(GraphicsDevice device, int width, int height)
        {
            // Half res looks terrible with the texels
            int w = width;
            int h = height;
            
            _ping = new RenderTarget2D(device, w, h, false, SurfaceFormat.HalfVector4, DepthFormat.None);
            _pong = new RenderTarget2D(device, w, h, false, SurfaceFormat.HalfVector4, DepthFormat.None);
        }

        private void DisposeTargets()
        {
            _ping?.Dispose();
            _pong?.Dispose();
            _ping = null;
            _pong = null;
        }
    }
}
