using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MagicEngine.Engine.Base.Shaders.PostProcessing.Implementations
{
    public class DualFilterBloom : PostProcessStep
    {
        // Customizable Parameters
        public float Threshold { get; set; } = 1.6f;
        public float SoftKnee { get; set; } = 0.5f;
        public float Intensity { get; set; } = 0.4f;

        // Internal State
        private const int MipCount = 6; // 1/2, 1/4, 1/8, 1/16, 1/32, 1/64
        private RenderTarget2D[] _mips;
        private int _lastWidth;
        private int _lastHeight;
        
        private Effect _effect;

        public DualFilterBloom(Effect effect)
        {
            _effect = effect;
            Type = EffectType.PixelLayer; // Bloom operates on final high-res but before tonemap ideally?
        }

        public override void Apply(SpriteBatch sb, Texture2D source, RenderTarget2D destination)
        {
            var device = sb.GraphicsDevice;
            int width = destination.Width;
            int height = destination.Height;

            // 1. Manage RenderTargets
            if (_mips == null || width != _lastWidth || height != _lastHeight)
            {
                DisposeMips();
                InitializeMips(device, width, height);
                _lastWidth = width;
                _lastHeight = height;
            }

            // Save state
            var originalBlendState = device.BlendState;
            var originalSamplerState = device.SamplerStates[0];
            
            // ---------------------------------------------------------------------
            // Downsample Chain
            // ---------------------------------------------------------------------
            
            // Pass 0: Extract + Downsample (Source -> Mip0)
            // Mip0 is Half Res
            _effect.CurrentTechnique = _effect.Techniques["ExtractAndDownsample"];
            _effect.Parameters["Threshold"]?.SetValue(Threshold);
            _effect.Parameters["SoftKnee"]?.SetValue(SoftKnee);
            // Karis Average is built-in
            
            DrawPass(sb, source, _mips[0], _effect, null, SamplerState.PointClamp);

            // Pass 1..N: Downsample (Mip[i] -> Mip[i+1])
            _effect.CurrentTechnique = _effect.Techniques["Downsample"];
            for (int i = 0; i < MipCount - 1; i++)
            {
               DrawPass(sb, _mips[i], _mips[i+1], _effect);
            }

            // ---------------------------------------------------------------------
            // Upsample Chain
            // ---------------------------------------------------------------------
            
            // Pass N..0: Upsample Mip[i] -> Blend into Mip[i-1]
            _effect.CurrentTechnique = _effect.Techniques["Upsample"];
            
            // IMPORTANT: Set Intensity to 1.0 for the intermediate passes to avoid compounding (Intensity^N).
            _effect.Parameters["Intensity"]?.SetValue(1.0f);
            
            for (int i = MipCount - 1; i > 0; i--)
            {
               DrawPass(sb, _mips[i], _mips[i-1], _effect, BlendState.Additive);
            }
            
            // Now _mips[0] contains the accumulated bloom.

            // ---------------------------------------------------------------------
            // Final Combine
            // ---------------------------------------------------------------------
            // Draw Source -> Destination (Opaque)
            // Draw Bloom (Mip0) -> Destination (Additive)
            
            device.SetRenderTarget(destination);
            
            // 1. Draw Original Source
            sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            sb.Draw(source, new Rectangle(0, 0, width, height), Color.White);
            sb.End();
            
            // 2. Draw Bloom
            _effect.CurrentTechnique = _effect.Techniques["Upsample"]; 
            _effect.Parameters["InverseResolution"]?.SetValue(new Vector2(1.0f / _mips[0].Width, 1.0f / _mips[0].Height));
            
            // Set actual user Intensity for the final mix
            _effect.Parameters["Intensity"]?.SetValue(Intensity); 
            
            var blendState = new BlendState {
                ColorSourceBlend = Blend.One,
                ColorDestinationBlend = Blend.One,
                AlphaSourceBlend = Blend.Zero,
                AlphaDestinationBlend = Blend.One
            };
            
            sb.Begin(SpriteSortMode.Immediate, blendState, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, _effect);
            sb.Draw(_mips[0], new Rectangle(0, 0, width, height), Color.White);
            sb.End();
        }

        private void DrawPass(SpriteBatch sb, Texture2D input, RenderTarget2D output, Effect effect, BlendState blendState = null, SamplerState samplerState = null)
        {
            var device = sb.GraphicsDevice;
            device.SetRenderTarget(output);
            
            // Set InverseResolution for the INPUT texture
            Vector2 invRes = new Vector2(1.0f / input.Width, 1.0f / input.Height);
            effect.Parameters["InverseResolution"]?.SetValue(invRes);
            
            sb.Begin(SpriteSortMode.Immediate, blendState ?? BlendState.Opaque, samplerState ?? SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, effect);
            sb.Draw(input, new Rectangle(0, 0, output.Width, output.Height), Color.White);
            sb.End();
        }

        private void InitializeMips(GraphicsDevice device, int width, int height)
        {
            _mips = new RenderTarget2D[MipCount];
            
            // Mip0 is half size
            int w = width / 2;
            int h = height / 2;
            
            for (int i = 0; i < MipCount; i++)
            {
                // Ensure size is at least 1x1
                w = Math.Max(1, w);
                h = Math.Max(1, h);
                
                // Use RGBA64 (HalfVector4) for HDR precision
                _mips[i] = new RenderTarget2D(device, w, h, false, SurfaceFormat.HalfVector4, DepthFormat.None);
                
                w /= 2;
                h /= 2;
            }
        }

        private void DisposeMips()
        {
            if (_mips != null)
            {
                foreach (var mip in _mips)
                    mip?.Dispose();
                _mips = null;
            }
        }
    }
}
