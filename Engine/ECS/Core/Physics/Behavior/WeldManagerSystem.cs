using System;
using DefaultEcs;
using MagicThing.Engine.Base.EntitySystem;
using MagicThing.Engine.Base.EntityWrappers;
using MagicThing.Engine.ECS.Core.Parenting.Components;
using MagicThing.Engine.ECS.Core.Physics.Behavior.Components;
using MagicThing.Engine.ECS.Core.Physics.Bridge.Components;
using MagicThing.Engine.ECS.Core.Positioning.Components;
using nkast.Aether.Physics2D.Dynamics.Joints;

namespace MagicThing.Engine.ECS.Core.Physics.Behavior;

/// <summary>
/// This system is responsible for
/// - IsWelded cleanup
/// - Physics world joint cleanup
/// </summary>
[UpdateInBucket(ExecutionBucket.Cleanup)]
public sealed class WeldManagerSystem : EntitySystem
{
    private IDisposable _hierarchyChangeEventSubscription;

    public override void OnSceneLoad()
    {
        _hierarchyChangeEventSubscription = World.Subscribe<HierarchyChangeEvent>(OnHierarchyChange);
    }

    public override void OnSceneUnload()
    {
        _hierarchyChangeEventSubscription.Dispose();
    }

    private void OnHierarchyChange(in HierarchyChangeEvent ev)
    {
        if (!ev.Actor.TryGet<PhysicsBodyComponent>(out var apb) || !ev.Parent.TryGet<PhysicsBodyComponent>(out var ppb))
            return;
        
        var childBody = apb.Comp.Body;
        var parentBody = ppb.Comp.Body;
            
        if (ev.Type == HierarchyChangeEventType.Attached)
        {
            var localOffset = ev.Actor.Has<LocalTransform>()
                ? ev.Actor.Get<LocalTransform>().Position
                : PhysicsSystem.ToECS(childBody.Position - parentBody.Position);
            
            var anchor = parentBody.Position;
            var weldJoint = JointFactory.CreateWeldJoint(
                PhysicsWorld, 
                parentBody, 
                childBody,
                parentBody.GetLocalPoint(anchor),
                childBody.GetLocalPoint(anchor));
            
            ev.Actor.Set(new IsWelded { Joint = weldJoint, Parent = ev.Parent });
            Console.WriteLine($"[WeldJointSystem] Created weld joint for Entity {ev.Actor} attached to {ev.Parent}.");
        }
        else
        {
            ref readonly var welded = ref ev.Actor.Get<IsWelded>();
            PhysicsWorld.Remove(welded.Joint);
            ev.Actor.TryRemComp<IsWelded>();
            Console.WriteLine($"[WeldJointSystem] Destroyed weld joint for Entity {ev.Actor}.");
        }
    }
    
    public override void Update(Timing timing)
    {
        var _query = World.GetEntities()
            .With<IsWelded>()
            .AsSet();
        
        foreach (ref readonly var entity in _query.GetEntities())
        {
            // If either the parent or the child dies, their physics bodies get removed
            // which will remove their joints. All we need to clean up here are the stale IsWelded components
            ref var current = ref entity.Get<IsWelded>();
            if (!current.Parent.IsAlive)
                entity.TryRemComp<IsWelded>();
        }
    }
}