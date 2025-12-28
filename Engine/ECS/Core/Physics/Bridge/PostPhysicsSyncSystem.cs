using DefaultEcs;
using DefaultEcs.System;
using MagicThing.Engine.Base.Events;
using MagicThing.Engine.ECS.Core.Events;
using MagicThing.Engine.ECS.Core.Physics.Bridge.Components;
using MagicThing.Engine.ECS.Core.Positioning.Components;
using nkast.Aether.Physics2D.Dynamics;
using World = DefaultEcs.World;

namespace MagicThing.Engine.ECS.Core.Physics.Bridge;

// System to synchronize state FROM Physics TO ECS, after the physics step.
public class PostPhysicsSyncSystem : ISystem<float>
{
    private readonly World _ecsWorld;
    private readonly nkast.Aether.Physics2D.Dynamics.World _physicsWorld;
    private readonly EntitySet _entitySet;
    private readonly EventManager _eventManager;

    public bool IsEnabled { get; set; } = true;

    public PostPhysicsSyncSystem(World ecsWorld, nkast.Aether.Physics2D.Dynamics.World physicsWorld, EventManager eventManager)
    {
        _ecsWorld = ecsWorld;
        _physicsWorld = physicsWorld;
        _eventManager = eventManager;
        
        _entitySet = ecsWorld.GetEntities()
            .With<Position>()
            .With<Velocity>()
            .With<PhysicsBodyComponent>()
            .AsSet();
    }

    public void Update(float state)
    {
        SyncEntityTransforms();
        ProcessCollisions();
    }

    private void ProcessCollisions()
    {
        foreach (var contact in _physicsWorld.ContactList)
        {
            if (!contact.IsTouching)
            {
                continue;
            }
            
            Body bodyA = contact.FixtureA.Body;
            Body bodyB = contact.FixtureB.Body;
            
            if (bodyA.Tag is not Entity entityA || bodyB.Tag is not Entity entityB)
            {
                continue;
            }
            
            _eventManager.Raise(entityA, new CollisionEvent(entityA, entityB, contact));
            _eventManager.Raise(entityB, new CollisionEvent(entityB, entityA, contact));
            
            _ecsWorld.Publish(new CollisionEvent(entityA, entityB, contact));
        }
    }

    private void SyncEntityTransforms()
    {
        foreach (var entity in _entitySet.GetEntities())
        {
            ref readonly var physicsBody = ref entity.Get<PhysicsBodyComponent>();
            
            ref var position = ref entity.Get<Position>();
            ref var velocity = ref entity.Get<Velocity>();
            
            position.Value = PhysicsSystem.ToECS(physicsBody.Body.Position);
            position.Rotation = physicsBody.Body.Rotation;
            velocity.Value = PhysicsSystem.ToECS(physicsBody.Body.LinearVelocity);
        }
    }

    public void Dispose()
    {
        _entitySet.Dispose();
    }
}