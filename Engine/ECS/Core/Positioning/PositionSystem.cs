using DefaultEcs;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.ECS.Core.Positioning.Components;

namespace MagicEngine.Engine.ECS.Core.Positioning;

// This system's only job is to snapshot the final position from the previous
// tick so the InterpolateSystem can use it later.
[UpdateInBucket(ExecutionBucket.PreUpdate)]
public sealed class PositionSystem : EntitySystem
{
    EntitySet? _query;

    public override void OnSceneLoad()
    {
        _query = World.GetEntities()
            .With<Position>()
            .With<PreviousPosition>()
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
            ref var previousPosition = ref entity.Get<PreviousPosition>();
            
            previousPosition.Value = position.Value;
            previousPosition.Rotation = position.Rotation;
        }
    }
}