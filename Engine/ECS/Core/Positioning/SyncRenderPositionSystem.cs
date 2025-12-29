using System;
using DefaultEcs;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.ECS.Core.Positioning.Components;
using Microsoft.Xna.Framework;

namespace MagicEngine.Engine.ECS.Core.Positioning;

[UpdateInBucket(ExecutionBucket.PreRender)]
public sealed class SyncRenderPositionSystem : EntitySystem
{
    private EntitySet? _query;

    public override void OnSceneLoad()
    {
        _query = World.GetEntities()
            .With<Position>()
            .With<RenderPosition>()
            .Without<PreviousPosition>() // That's how we know it's not interpolated
            .AsSet();
    }

    public override void OnSceneUnload()
    {
        _query?.Dispose();
    }

    public override void Update(Timing timing)
    {
        foreach (ref readonly var entity in _query.GetEntities())
        {
            ref readonly var position = ref entity.Get<Position>();
            ref var renderPosition = ref entity.Get<RenderPosition>();
            
            renderPosition.Value = new Vector2(
                (int)Math.Round(position.Value.X),
                (int)Math.Round(position.Value.Y)
                );
            renderPosition.Rotation = position.Rotation;
        }
    }
}