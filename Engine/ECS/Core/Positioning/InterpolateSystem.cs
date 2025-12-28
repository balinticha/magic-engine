using System;
using DefaultEcs;
using MagicThing.Engine.Base.EntitySystem;
using MagicThing.Engine.ECS.Core.Positioning.Components;
using Microsoft.Xna.Framework;

namespace MagicThing.Engine.ECS.Core.Positioning;

[UpdateInBucket(ExecutionBucket.PreRender)]
public sealed class InterpolateSystem : EntitySystem
{
    public override void Update(Timing timing)
    {
        var _query = World.GetEntities()
            .With<Position>()
            .With<RenderPosition>()
            .With<PreviousPosition>()
            .AsSet();
        
        var alpha = timing.Alpha;

        foreach (ref readonly var entity in _query.GetEntities())
        {
            ref readonly var current = ref entity.Get<Position>();
            ref readonly var previous = ref entity.Get<PreviousPosition>();
            ref var render = ref entity.Get<RenderPosition>();
            
            var interpolated = Vector2.Lerp(previous.Value, current.Value, alpha);

            render.Value = new Vector2(
                (int)Math.Round(interpolated.X),
                (int)Math.Round(interpolated.Y));

            render.Rotation = MathHelper.Lerp(previous.Rotation, current.Rotation, alpha);
        }
    }
}