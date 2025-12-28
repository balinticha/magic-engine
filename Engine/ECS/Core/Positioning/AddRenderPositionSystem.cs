using System;
using DefaultEcs;
using MagicThing.Engine.Base.EntitySystem;
using MagicThing.Engine.ECS.Core.Positioning.Components;
using Microsoft.Xna.Framework;

namespace MagicThing.Engine.ECS.Core.Positioning;

[UpdateInBucket(ExecutionBucket.Cleanup)]
public sealed class AddRenderPositionSystem : EntitySystem
{
    public override void Update(Timing timing)
    {
        var _query = World.GetEntities()
            .With<Position>()
            .Without<RenderPosition>()
            .AsSet();
        
        foreach (ref readonly var entity in _query.GetEntities())
        {
            ref readonly var pos = ref entity.Get<Position>();

            var rp = new RenderPosition
            {
                Value = new Vector2
                {
                    X = (int)Math.Round(pos.Value.X),
                    Y = (int)Math.Round(pos.Value.Y),
                },
                Rotation = pos.Rotation
            };
            
            entity.Set(rp);
        }
    }
}