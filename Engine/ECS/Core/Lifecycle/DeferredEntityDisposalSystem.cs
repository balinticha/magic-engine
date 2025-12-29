using DefaultEcs;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.ECS.Core.Lifecycle.Components;

namespace MagicEngine.Engine.ECS.Core.Lifecycle;

[UpdateInBucket(ExecutionBucket.Cleanup)]
public class DeferredEntityDisposalSystem : EntitySystem
{
    private EntitySet? _query;
    
    public override void OnSceneLoad()
    {
        _query = _query = World.GetEntities().With<MarkedForDeath>().AsSet();;
    }

    public override void OnSceneUnload()
    {
        _query.Dispose();
    }

    public override void Update(Timing timing)
    {
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