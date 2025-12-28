using DefaultEcs;
using MagicThing.Engine.Base.EntitySystem;
using MagicThing.Engine.Base.EntityWrappers;
using MagicThing.Engine.ECS.Core.Physics;
using MagicThing.Engine.ECS.Core.Physics.Bridge.Components;
using MagicThing.Engine.ECS.Core.Positioning.Components;

namespace MagicThing.Engine.ECS.Core.Positioning;

[UpdateInBucket(ExecutionBucket.Update)]
public sealed class ProcessPositionRequestSystem : EntitySystem
{
    public void ManualUpdate()
    {
        var _query = World.GetEntities()
            .With<Position>()
            .With<Velocity>()
            .With<SetPositionRequest>()
            .AsSet();
        
        foreach (ref readonly var entity in _query.GetEntities())
        {
            ref var pos = ref entity.Get<Position>();
            ref var vel = ref entity.Get<Velocity>();
            ref var req = ref entity.Get<SetPositionRequest>();

            if (entity.TryGet<PhysicsBodyComponent>(out var pb))
            {
                ref var comp = ref pb.Comp;
                comp.Body.Position = PhysicsSystem.ToPhysics(req.RequestPosition);
                vel.Value += req.RequestVelocityChange;
                entity.TryRemComp<SetPositionRequest>();
                continue;
            }
            
            pos.Value = req.RequestPosition;
            vel.Value += req.RequestVelocityChange;
            entity.TryRemComp<SetPositionRequest>();
        }
    }
}