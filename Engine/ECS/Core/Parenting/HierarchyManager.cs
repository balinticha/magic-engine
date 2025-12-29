using System;
using DefaultEcs;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.ECS.Core.Parenting.Components;
using MagicEngine.Engine.ECS.Core.Positioning.Components;
using MagicEngine.Engine.Base.EntityWrappers;

namespace MagicEngine.Engine.ECS.Core.Parenting;

/// <summary>
/// This system is responsible for:
/// - IsParent and IsChildren component cleanup
/// - LocalTransform
/// - Providing a public API for centralized attach / detach operations
/// - Calling HierarchyChangeEvent if the API is used and stuff changes
/// </summary>
[UpdateInBucket(ExecutionBucket.Cleanup)]
public class HierarchyManager : EntitySystem
{
    private EntitySet? _query;
    
    public override void OnSceneLoad()
    {
        _query = World.GetEntities()
            .WithEither<IsParent>()
            .Or<IsChildren>()
            .AsSet();
    }

    public override void OnSceneUnload()
    {
        _query.Dispose();
    }

    public override void Update(Timing timing)
    {
        foreach (ref readonly var entity in _query.GetEntities())
        {
            if (entity.TryGet<IsChildren>(out var parent))
            {
                if (!parent.Comp.Parent.IsAlive)
                    // Parent is dead, we need to clean up
                    // Since the parent is dead, we only need to clean up this entity
                    entity.TryRemComp<IsChildren>();
            }

            if (entity.TryGet<IsParent>(out var children))
            {
                var childList = children.Comp.Childrens;

                for (int i = childList.Count - 1; i >= 0; i--)
                {
                    var child = childList[i];
                    if (!child.IsAlive)
                    {
                        childList.RemoveAt(i);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Parent the actor to the target. If the actor is already attached to something, do nothing.
    /// </summary>
    /// <param name="actor">Entity being attached.</param>
    /// <param name="target">Entity the actor is being attached to</param>
    /// <returns>Outcome</returns>
    public bool TryAttach(Entity actor, Entity target, LocalTransform? desiredOffset = null)
    {
        // no.
        if (actor == target)
            return false;
        
        if (actor.Has<IsChildren>())
            return false;

        if (!actor.IsAlive || !target.IsAlive)
            return false;
        
        AttachUnsafe(actor, target, desiredOffset);
        return true;
    }

    /// <summary>
    /// Parent the actor to the target without safety checks.
    /// </summary>
    /// <param name="actor">Entity being attached</param>
    /// <param name="target">Entity the actor is being attached to</param>
    /// <returns></returns>
    public void AttachUnsafe(Entity actor, Entity target, LocalTransform? desiredOffset = null)
    {
        // still ABSOLUTELY no
        if (actor == target)
            throw new InvalidOperationException("Actor cannot target themselves.");
        
        actor.Set(new IsChildren
        {
            Parent = target,
        });

        if (target.TryGet<IsParent>(out var parentComp))
        {
            // target is already a parent, add!
            parentComp.Comp.Childrens.Add(actor);
        }
        else
        {
            // new parent!
            target.Set(new IsParent
            {
                Childrens = [actor]
            });
        }
        
        // Physics engine stuff
        if (desiredOffset.HasValue)
        {
            actor.Set(desiredOffset.Value);
        }
        else if (!actor.Has<LocalTransform>())
        {
            ref readonly var actorPos = ref actor.Get<Position>();
            ref readonly var targetPos = ref target.Get<Position>();
            actor.Set(new LocalTransform { Position = actorPos.Value - targetPos.Value });
        }
        
        World.Publish(new HierarchyChangeEvent
        {
            Type = HierarchyChangeEventType.Attached,
            Actor = actor,
            Parent = target,
        });
    }

    /// <summary>
    /// Detach the actor (children) from the target (parent).
    /// </summary>
    public bool TryDetach(Entity actor, Entity target)
    {
        // Actor not a children
        if (!actor.TryGet<IsChildren>(out var childrenComp))
            return false;
        
        // Actor is not the children of the parent
        if (!(childrenComp.Comp.Parent == target))
            return false;

        // Target is not a parent
        if (!target.TryGet<IsParent>(out var parentComp))
            return false;

        // Target is not the parent of actor
        if (!parentComp.Comp.Childrens.Contains(actor))
            return false;

        // boop
        parentComp.Comp.Childrens.Remove(actor);
        actor.TryRemComp<IsChildren>();

        World.Publish(new HierarchyChangeEvent
        {
            Type = HierarchyChangeEventType.Detached,
            Actor = actor,
            Parent = target,
        });
        
        return true;
    }

    /// <summary>
    /// Safely switch the parent of an entity.
    /// </summary>
    /// <returns>Outcome</returns>
    public bool TrySafeSwitchParent(Entity actor, Entity currentParent, Entity newParent, LocalTransform? desiredOffset = null)
    {
        if (!TryDetach(actor, currentParent))
            return false;
        
        AttachUnsafe(actor, newParent, desiredOffset);
        return true;
    }
}