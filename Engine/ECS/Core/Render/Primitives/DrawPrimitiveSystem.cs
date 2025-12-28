using DefaultEcs;
using MagicThing.Engine.Base.EntitySystem;
using MagicThing.Engine.Base.EntityWrappers;
using MagicThing.Engine.ECS.Core.Positioning.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace MagicThing.Engine.ECS.Core.Render.Primitives;

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