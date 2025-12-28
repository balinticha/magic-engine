using DefaultEcs;
using MagicThing.Engine.Base.EntitySystem;
using MagicThing.Engine.ECS.Core.Positioning.Components;

namespace MagicThing.Engine.ECS.Core.Positioning;

// This system's only job is to snapshot the final position from the previous
// tick so the InterpolateSystem can use it later.
[UpdateInBucket(ExecutionBucket.PreUpdate)]
public sealed class PositionSystem : EntitySystem
{
    public override void Update(Timing timing)
    {
        var _query = World.GetEntities()
            .With<Position>()
            .With<PreviousPosition>()
            .AsSet();
        
        foreach (ref readonly var entity in _query.GetEntities())
        {
            ref readonly var position = ref entity.Get<Position>();
            ref var previousPosition = ref entity.Get<PreviousPosition>();
            
            previousPosition.Value = position.Value;
            previousPosition.Rotation = position.Rotation;
        }
    }
}