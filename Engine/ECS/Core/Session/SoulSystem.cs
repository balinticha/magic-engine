using System;
using DefaultEcs;
using MagicThing.Engine.Base.EntitySystem;
using MagicThing.Engine.ECS.Core.Session.Components;

namespace MagicThing.Engine.ECS.Core.Session;

[UpdateInBucket(ExecutionBucket.Cleanup)]
public sealed class SoulSystem : EntitySystem
{
    [Dependency] private readonly SessionManager _sessionManager;

    public override void Update(Timing timing)
    {
        var _query = World.GetEntities()
            .With<Soul>()
            .AsSet();
        
        // check if the player no longer controls an entity
        if (!_sessionManager.GetControlledEntity(out _))
        {
            // Not controlling an entity or the entity is dead
            var ent = Prototypes.SpawnEntity("Engine/Soul", _sessionManager.ControlledEntityLastPosition);
            Console.WriteLine($"[SoulSystem] Session player is deattached from an entity. Soul entity spawned: {ent}");
            
            _sessionManager.SetControlledEntity(ent);
        }
        
        // Checks for souls no longer controlled
        foreach (ref readonly var entity in _query.GetEntities())
        {
            if (entity == _sessionManager.GetControlledEntityUnsafe())
                continue;
            
            entity.Dispose();
        }
    }
}