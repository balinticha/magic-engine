using System;
using System.Collections.Generic;
using DefaultEcs;
using DefaultEcs.System;
using MagicEngine.Engine.ECS.Core.Physics.Bridge.Components;
using nkast.Aether.Physics2D.Dynamics;
using World = nkast.Aether.Physics2D.Dynamics.World;

namespace MagicEngine.Engine.ECS.Core.Physics.Bridge;

/// <summary>
/// Reacts to the removal of PhysicsBodyComponent from entities and safely
/// removes the corresponding Body from the Aether2D physics world.
/// </summary>
public class PhysicsBodyDeletionSystem : ISystem<float>
{
    private readonly World _physicsWorld;
    private readonly IDisposable _componentRemovedSubscription;
    
    // Using a Queue is a clean way to manage bodies waiting for deletion.
    private readonly Queue<Body> _bodiesToRemove;

    public bool IsEnabled { get; set; } = true;

    public PhysicsBodyDeletionSystem(DefaultEcs.World ecsWorld, World physicsWorld)
    {
        _physicsWorld = physicsWorld;
        _bodiesToRemove = new Queue<Body>();

        // Subscribe to the event. When a PhysicsBodyComponent is removed,
        // the OnPhysicsBodyRemoved method will be called.
        _componentRemovedSubscription = ecsWorld.SubscribeComponentRemoved<PhysicsBodyComponent>(OnPhysicsBodyRemoved);
    }

    /// <summary>
    /// This method is the callback for the subscription. It's called automatically by DefaultEcs.
    /// </summary>
    /// <param name="entity">The entity from which the component was removed.</param>
    /// <param name="removedComponent">The instance of the component that was removed.</param>
    private void OnPhysicsBodyRemoved(in Entity entity, in PhysicsBodyComponent removedComponent)
    {
        if (removedComponent.Body != null)
        {
            _bodiesToRemove.Enqueue(removedComponent.Body);
            Console.WriteLine($"[BodyDeletionSystem] Queued physics body for removal from Entity {entity}.");
        }
    }

    /// <summary>
    /// This Update method should be called AFTER the physics world step.
    /// </summary>
    public void Update(float state)
    {
        // Process all bodies that were queued for deletion.
        while (_bodiesToRemove.Count > 0)
        {
            var body = _bodiesToRemove.Dequeue();

            // Safety check: ensure the body is actually in the world before trying to remove it.
            if (body.World == _physicsWorld)
            {
                _physicsWorld.Remove(body);
            }
        }
    }

    /// <summary>
    /// Clean up the subscription when the system is disposed.
    /// </summary>
    public void Dispose()
    {
        _componentRemovedSubscription.Dispose();
    }
}