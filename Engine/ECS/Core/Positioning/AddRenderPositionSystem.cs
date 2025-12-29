using System;
using DefaultEcs;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.ECS.Core.Positioning.Components;
using Microsoft.Xna.Framework;

namespace MagicEngine.Engine.ECS.Core.Positioning;

[UpdateInBucket(ExecutionBucket.Cleanup)]
public sealed class AddRenderPositionSystem : EntitySystem
{
    private EntitySet? _query;
    
    public override void OnSceneLoad()
    {
        _query = World.GetEntities()
            .With<Position>()
            .Without<RenderPosition>()
            .AsSet();
    }

    public override void OnSceneUnload()
    {
        _query.Dispose();
    }

    public override void Update(Timing timing)
    {
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