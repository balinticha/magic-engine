using System;
using System.Collections.Generic;
using DefaultEcs;
using MagicEngine.Engine.Base;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.ECS.Core.Positioning.Components;
using MagicEngine.Engine.ECS.Core.Render.Components;
using MagicEngine.Engine.Base.Debug;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MagicEngine.Engine.ECS.Core.Render;

// TODO This renderer implementation is just fine for now, HOWEVER:
// In the long run, two critical features must be implemented:
// - Texture Atlases
// - Vertex data support -> get rid of SpriteBatch but this is future future stuff
[UpdateInBucket(ExecutionBucket.Render)]
public class SpriteDrawSystem : EntitySystem
{
    private readonly List<RenderItem> _renderQueue = new(2048);
    private Effect _defaultShader;
    private int _batchCount = 0;

    public override void OnSceneLoad()
    {
        // this is not in the constructor because Content seems to not be available at that point
        _defaultShader = Content.Load<Effect>("Effects/SpriteBase");
    }

    private static readonly BlendState BlendAdditivePremultiplied = new()
    {
        Name = "AdditivePremultiplied",
        ColorSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.One,
        AlphaDestinationBlend = Blend.One
    };
    
    private struct RenderItem : IComparable<RenderItem>
    {
        public Entity Entity;
        public Texture2D Texture;
        public Effect Effect;
        public Dictionary<string, object> Parameters;
        public int ParamHash;
        public MaterialBlendMode BlendMode;
        public Vector2 Position; // cached Position.Value
        public float Rotation;
        public Color Color;
        public Vector2 Origin;
        public int Layer;
        public float Z;
        public Vector2 DestinationSize;

        public int CompareTo(RenderItem other)
        {
            int layerCompare = Layer.CompareTo(other.Layer);
            if (layerCompare != 0) return layerCompare;

            int zCompare = Z.CompareTo(other.Z);
            if (zCompare != 0) return zCompare;

            int thisHash = Effect?.GetHashCode() ?? 0;
            int otherHash = other.Effect?.GetHashCode() ?? 0;
            
            int effectCompare = thisHash.CompareTo(otherHash);
            if (effectCompare != 0) return effectCompare;

            int blendCompare = (int)BlendMode - (int)other.BlendMode;
            if (blendCompare != 0) return blendCompare;

            return ParamHash.CompareTo(other.ParamHash);
        }
    }

    public override void Draw(Timing timing, SpriteBatch spriteBatch, Matrix transformMatrix)
    {
        var spriteEntities = World.GetEntities()
            .With<RenderPosition>()
            .With<Sprite>()
            .AsSet();
            
        var boundsEntities = World.GetEntities()
            .With<RenderPosition>()
            .With<RenderBounds>()
            .With<Material>()
            .Without<Sprite>()
            .AsSet();

        _renderQueue.Clear();
        _batchCount = 0;
        
        // culling
        // inverting the transform gives us the World Coordinates of the screen corners
        var viewport = spriteBatch.GraphicsDevice.Viewport;
        Matrix inverseTransform = Matrix.Invert(transformMatrix);

        float margin = 300f;
        
        Vector2 tl = Vector2.Transform(Vector2.Zero, inverseTransform);
        Vector2 tr = Vector2.Transform(new Vector2(viewport.Width, 0), inverseTransform);
        Vector2 bl = Vector2.Transform(new Vector2(0, viewport.Height), inverseTransform);
        Vector2 br = Vector2.Transform(new Vector2(viewport.Width, viewport.Height), inverseTransform);
        
        float minX = Math.Min(Math.Min(tl.X, tr.X), Math.Min(bl.X, br.X));
        float maxX = Math.Max(Math.Max(tl.X, tr.X), Math.Max(bl.X, br.X));
        float minY = Math.Min(Math.Min(tl.Y, tr.Y), Math.Min(bl.Y, br.Y));
        float maxY = Math.Max(Math.Max(tl.Y, tr.Y), Math.Max(bl.Y, br.Y));
        
        float viewLeft = minX - margin;
        float viewRight = maxX + margin;
        float viewTop = minY - margin;
        float viewBottom = maxY + margin;

        void ProcessEntity(ref readonly Entity entity, Texture2D texture, Color color, Vector2 anchor, float width, float height, int layer, float sortOffset)
        { 
            ref var pos = ref entity.Get<RenderPosition>();
                
            // Frustum Culling
            // rough size estimation. 
            if (texture == null) return;
                
            float halfW = width / 2.0f;
            float halfH = height / 2.0f;
                
            float spriteLeft = pos.Value.X - halfW;
            float spriteRight = pos.Value.X + halfW;
            float spriteTop = pos.Value.Y - halfH;
            float spriteBottom = pos.Value.Y + halfH;
            
            // Fast rejection path
            // If the sprite is to the Left of the view... OR to the Right... OR Above... etc.
            if (spriteRight < viewLeft || 
                spriteLeft > viewRight || 
                spriteBottom < viewTop || 
                spriteTop > viewBottom)
            {
                return;  // bye
            }
    
            Effect effect = null;
            Dictionary<string, object> parameters = null;
            int paramHash = 0;
    
            if (entity.Has<Material>())
            {
                ref var mat = ref entity.Get<Material>();
                effect = mat.Effect;
                parameters = mat.Parameters;
                    
                // fuck it, we hash
                // to-do maybe mine crypto while at it :p
                mat.ForcedUpdateHash();
                paramHash = mat.GetCachedHash;
            }
    
            float intensity = 1.0f;
            if (entity.Has<Sprite>())
            {
                 intensity = entity.Get<Sprite>().Intensity;
            }
            if (entity.Has<Material>())
            {
                intensity = entity.Get<Material>().Intensity;
            }

            _renderQueue.Add(new RenderItem
            {
                Entity = entity,
                Texture = texture,
                Effect = effect,
                Parameters = parameters,
                ParamHash = paramHash,
                BlendMode = entity.Has<Material>() ? entity.Get<Material>().BlendMode : MaterialBlendMode.AlphaBlend,
                Position = pos.Value,
                Rotation = pos.Rotation,
                Color = color * intensity,
                Origin = anchor, 
                Layer = layer,
                Z = pos.Value.Y + sortOffset,
                DestinationSize = new Vector2(width, height)
            });
        }
        
        foreach (ref readonly var entity in spriteEntities.GetEntities())
        {
            ref var sprite = ref entity.Get<Sprite>();
            if (sprite.Texture == null) continue;
            
            ProcessEntity(in entity, sprite.Texture, sprite.Color, 
                new Vector2(sprite.Texture.Width * 0.5f, sprite.Texture.Height * 0.5f), 
                sprite.Texture.Width, sprite.Texture.Height,
                sprite.Layer, sprite.SortOffset);
        }
        
        foreach (ref readonly var entity in boundsEntities.GetEntities())
        {
            ref var bounds = ref entity.Get<RenderBounds>();
            // For material-only entities, we use WhitePixel (1x1)
            // Since we scale the 1x1 texture by bounds.Width/Height, the Origin must be in local texture space (0..1)
            // so that Scale * Origin results in the correct pixel offset.
            Vector2 origin = bounds.Anchor;
            
            ProcessEntity(in entity, MagicGame.WhitePixel, Color.White, 
                origin, 
                bounds.Width, bounds.Height, 
                bounds.Layer, bounds.SortOffset); 
        }
        
        _renderQueue.Sort();
        
        Effect currentEffect = null;
        int currentParamHash = 0;
        MaterialBlendMode currentBlendMode = MaterialBlendMode.AlphaBlend;
        bool isBatchRunning = false;
        
        void BeginBatch(Effect effect, int paramHash, MaterialBlendMode blendMode)
        {
            _batchCount += 1;
            if (isBatchRunning) spriteBatch.End();
            Effect finalEffect = effect ??  _defaultShader;

            BlendState blendState = BlendState.AlphaBlend;
            switch (blendMode)
            {
                case MaterialBlendMode.Additive: blendState = BlendAdditivePremultiplied; break;
                case MaterialBlendMode.NonPremultiplied: blendState = BlendState.NonPremultiplied; break;
                case MaterialBlendMode.Opaque: blendState = BlendState.Opaque; break;
            }
            
            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: blendState,
                samplerState: SamplerState.PointClamp,
                depthStencilState: DepthStencilState.None,
                rasterizerState: RasterizerState.CullNone,
                effect: finalEffect,
                transformMatrix: transformMatrix
            );
            isBatchRunning = true;
            currentEffect = effect;
            currentParamHash = paramHash;
            currentBlendMode = blendMode;
        }
        
        // Close the incoming batch from MagicGame before starting our own sorted batches
        spriteBatch.End();
        BeginBatch(null, 0, MaterialBlendMode.AlphaBlend);
        
        foreach (var item in _renderQueue)
        {
            if (item.Effect != currentEffect || item.ParamHash != currentParamHash || item.BlendMode != currentBlendMode)
            {
                if (item.Effect != null)
                {
                    // Flush the previous batch *before* changing the effect parameters
                    // to ensure the previous items use the old parameters.
                    if (isBatchRunning)
                    {
                         spriteBatch.End();
                         isBatchRunning = false;
                    }
                    ApplyMaterialParameters(item.Entity, item.Effect, timing);
                }

                BeginBatch(item.Effect, item.ParamHash, item.BlendMode);
            }

            // If using WhitePixel (1x1), we must scale it to DestinationSize
            Vector2 scale = Vector2.One;
            if (item.Texture == MagicGame.WhitePixel)
            {
                scale = item.DestinationSize;
            }

            spriteBatch.Draw(
                texture: item.Texture,
                position: item.Position,
                sourceRectangle: null,
                color: item.Color,
                rotation: item.Rotation,
                origin: item.Origin, 
                scale: scale,
                effects: SpriteEffects.None,
                layerDepth: 0f // handle depth via draw order
            );
        }

        if (isBatchRunning) spriteBatch.End();

        // restore default state for next systems
        spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: transformMatrix);

        if (_batchCount >= 800)
        {
            LogManager.Log("Drawing too many calls!");
        }
    }


    
    private void ApplyMaterialParameters(Entity entity, Effect effect, Timing timing)
    {
         if (!entity.Has<Material>()) return;
         
         ref var material = ref entity.Get<Material>();

         // Global Params
         var timeParam = effect.Parameters["Time"];
         if (timeParam != null)
         {
             timeParam.SetValue((float)timing.GameTime);
         }

         // Material Params
         if (material.Parameters != null)
         {
             foreach (var kvp in material.Parameters)
             {
                 var param = effect.Parameters[kvp.Key];
                 if (param == null) continue;
    
                 object val = kvp.Value;
                 
                 switch (val)
                 {
                     case float f: param.SetValue(f); break;
                     case int i: param.SetValue((float)i); break;
                     case Vector2 v2: param.SetValue(v2); break;
                     case Vector3 v3: param.SetValue(v3); break;
                     case Vector4 v4: param.SetValue(v4); break;
                     case Color c: param.SetValue(c.ToVector4()); break;
                     case Texture2D t: param.SetValue(t); break;
                 }
             }
         }
    }
}