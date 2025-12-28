using DefaultEcs;
using DefaultEcs.System;
using MagicEngine.Engine.ECS.Core.Physics.Bridge.Components;
using MagicEngine.Engine.ECS.Core.Positioning.Components;

namespace MagicEngine.Engine.ECS.Core.Physics.Bridge;

// System to synchronize state FROM ECS TO Physics, before the physics step.
public class PrePhysicsSyncSystem : AEntitySetSystem<float>
{
    public PrePhysicsSyncSystem(World ecsWorld)
        // Subscribe to all entities that have both a VelocityComponent and a PhysicsBodyComponent.
        : base(ecsWorld.GetEntities()
            .With<Velocity>()
            .With<Position>()
            .With<PhysicsBodyComponent>()
            .AsSet())
            
    {
    }

    protected override void Update( float state, in Entity entity)
    {
        // Get the velocity from the ECS component
        ref readonly var velocity = ref entity.Get<Velocity>();
        ref readonly var position = ref entity.Get<Position>();
        // Get the physics body from the physics component
        ref readonly var physicsBody = ref entity.Get<PhysicsBodyComponent>();

        // Copy the value. This ensures game logic (e.g., from input) drives the physics velocity.
        physicsBody.Body.LinearVelocity = PhysicsSystem.ToPhysics(velocity.Value);
        physicsBody.Body.Rotation = position.Rotation;
        //physicsBody.Body.Position = PhysicsSystem.ToPhysics(position.Value);
    }
}