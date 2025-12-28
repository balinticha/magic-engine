using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.ECS.Core.Lifecycle.Components;

namespace MagicEngine.Engine.ECS.Core.Lifecycle;

[UpdateInBucket(ExecutionBucket.Cleanup)]
public class DeferredEntityDisposalSystem : EntitySystem
{
    public override void Update(Timing timing)
    {
        var _query = World.GetEntities()
            .With<MarkedForDeath>()
            .AsSet();

        string removedEntities = "";
        
        foreach (var entity in _query.GetEntities())
        {
            if (entity.IsAlive)
            {
                removedEntities += $"{($"{entity}").Substring(7)}E ";
                entity.Dispose();
            }
        }

        if (removedEntities != "")
        {
            Vx($"Removed these entities at end of frame: {removedEntities}");
        }
        
    }
}