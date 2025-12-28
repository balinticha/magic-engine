using DefaultEcs;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.ECS.Core.Positioning.Components;
using MagicEngine.Engine.Base.EntityWrappers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace MagicEngine.Engine.ECS.Core.Render.Primitives;

[UpdateInBucket(ExecutionBucket.Render)]
public sealed class DrawPrimitiveSystem : EntitySystem
{
    public override void Draw(Timing timing, SpriteBatch spriteBatch, Matrix transform)
    {
        var _query = World.GetEntities()
            .WithEither<DrawRectangle>()
            .Or<DrawCircle>()
            .With<RenderPosition>()
            .AsSet();
        
        foreach (ref readonly var entity in _query.GetEntities())
        {
            ref readonly var pos = ref entity.Get<RenderPosition>().Value;

            if (entity.TryGet<DrawRectangle>(out var cmp_r))
            {
                var drawCenter = pos + cmp_r.Comp.Offset;
                var drawPos = new Vector2(
                    drawCenter.X - cmp_r.Comp.Width / 2f,
                    drawCenter.Y - cmp_r.Comp.Height / 2f);
                
                spriteBatch.FillRectangle(drawPos.X, drawPos.Y, cmp_r.Comp.Width, cmp_r.Comp.Height, cmp_r.Comp.Color);
            }

            if (entity.TryGet<DrawCircle>(out var cmp_c))
            {
                var drawCenter = pos + cmp_c.Comp.Offset;
                
                spriteBatch.DrawCircle(drawCenter, cmp_c.Comp.Radius, 15, cmp_c.Comp.Color, 5f);
            }
        }
    }
}