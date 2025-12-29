using DefaultEcs;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.ECS.Core.Physics;
using MagicEngine.Engine.ECS.Core.Physics.Bridge.Components;
using MagicEngine.Engine.ECS.Core.Positioning.Components;
using MagicEngine.Engine.Base.EntityWrappers;

namespace MagicEngine.Engine.ECS.Core.Positioning;

[UpdateInBucket(ExecutionBucket.Update)]
public sealed class ProcessPositionRequestSystem : EntitySystem
{
    EntitySet? _query;

    public override void OnSceneLoad()
    {
        _query = World.GetEntities()
            .With<Position>()
            .With<Velocity>()
            .With<SetPositionRequest>()
            .AsSet();
    }

    public override void OnSceneUnload()
    {
        _query?.Dispose();
    }

    public void ManualUpdate()
    {
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