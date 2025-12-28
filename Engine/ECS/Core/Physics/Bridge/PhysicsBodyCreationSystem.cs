using System;
using DefaultEcs;
using DefaultEcs.System;
using MagicThing.Engine.Base.EntityWrappers;
using MagicThing.Engine.ECS.Core.Physics.Bridge.Components;
using MagicThing.Engine.ECS.Core.Positioning.Components;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Dynamics;
using World = nkast.Aether.Physics2D.Dynamics.World;

namespace MagicThing.Engine.ECS.Core.Physics.Bridge;

public class PhysicsBodyCreationSystem: AEntitySetSystem<float>
{
    private readonly World _physicsWorld;

    public PhysicsBodyCreationSystem(DefaultEcs.World ecsWorld, World physicsWorld)
        : base(ecsWorld.GetEntities()
            .With<Position>()
            // TODO not hardcode this
            .With<RectangleColliderComponent>()
            .Without<PhysicsBodyComponent>()
            .AsSet())
    {
        _physicsWorld = physicsWorld;
    }
    
    protected override void Update(float state, in Entity entity)
    {
        // Get data from the entity's components
        ref readonly var pos = ref entity.Get<Position>();
        ref readonly var collider = ref entity.Get<RectangleColliderComponent>();
        
        float density = 1.0f; // Default density if no material is specified
        if (entity.TryGet<PhysicsMaterialComponent>(out var material))
        {
            density = material.Comp.Density;
        }
        
        // Create the physics body in the Aether world
        // The body's initial position is taken from the PositionComponent.
        var body = _physicsWorld.CreateBody(
            new Vector2(
                pos.Value.X * PhysicsConstants.MetersPerPixel,
                pos.Value.Y * PhysicsConstants.MetersPerPixel)
        );
        
        body.Rotation = pos.Rotation;
        body.FixedRotation = entity.Has<FixedRotation>();
        
        var fixture = body.CreateRectangle(
            PhysicsSystem.ToPhysics(collider.Width), 
            PhysicsSystem.ToPhysics(collider.Height), 
            density, 
            new Vector2(
                collider.Offset.X * PhysicsConstants.MetersPerPixel,
                collider.Offset.Y * PhysicsConstants.MetersPerPixel)
        );
        fixture.IsSensor = collider.IsSensor;

        if (entity.Has<CollisionFilterComponent>())
        {
            ref readonly var filter = ref entity.Get<CollisionFilterComponent>();
            
            fixture.CollisionCategories = (Category)filter.Category;

            if (filter.CollidesWith != CollisionCategory.None)
            {
                fixture.CollidesWith = (Category)filter.CollidesWith;
            }
            else
            {
                fixture.CollidesWith = (Category)CollisionRules.GetCollidesWith(filter.Category);
            }
        }
        else
        {
            fixture.CollisionCategories = (Category)1;
            fixture.CollidesWith = Category.None;
        }
        
        body.Tag = entity;

        // Add the PhysicsBodyComponent to the entity, storing the new body.
        // This also ensures this system won't process this entity again.
        entity.Set(new PhysicsBodyComponent { Body = body });

        Console.WriteLine($"[BodyCreationSystem] Created physics body for Entity {entity}.");
    }
}