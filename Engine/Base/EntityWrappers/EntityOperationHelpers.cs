using System;
using System.Collections.Generic;
using System.Linq;
using DefaultEcs;
using MagicEngine.Engine.ECS.Core.Events.EntityDeath;
using MagicEngine.Engine.ECS.Core.Lifecycle.Components;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.Base.PrototypeComponentSystem;

namespace MagicEngine.Engine.Base.EntityWrappers;

public class EntityOperationHelpers : EntitySystem.EntitySystem
{
    /// <summary>
    /// Kill an entity for gameplay reasons.
    /// Any system can prevent this, which should not cause an issue
    /// (think of: death prevented by extra life pickup)
    /// </summary>
    public bool TryKillEntity(Entity ent)
    {
        if (ent.Has<MarkedForDeath>())
        {
            return true;
        }
        
        if (!ent.IsAlive)
        {
            return false;
        }
        
        if (Events.Raise(ent, new EntityDeathRequestEvent(ent)).IsCancelled)
            return false;
        
        Events.Raise(ent, new EntityDeathEvent(ent));
        ent.Set<MarkedForDeath>();
        Vx($"Entity {ent} marked for deletion");
        return true;
    }

    /// <summary>
    /// Kill an entity for cleanup reasons.
    /// Systems can prevent this, but will crash the application if they do so.
    /// of said entity.
    /// </summary>
    public void ForceKillEntity(Entity ent)
    {
        if (!ent.IsAlive || ent.Has<MarkedForDeath>())
        {
            return;
        }
        
        var ev = Events.Raise(ent, new ForcedEntityDeathRequestEvent(ent));
        if (ev.CancelAndRaiseFatalError)
            AssertFailure($"Tried to force kill an entity: {ev.ErrorMessage} but it was cancelled");
        
        Events.Raise(ent, new EntityDeathEvent(ent));
        ent.Set<MarkedForDeath>();
        Vx($"Entity {ent} marked for deletion");
    }
}