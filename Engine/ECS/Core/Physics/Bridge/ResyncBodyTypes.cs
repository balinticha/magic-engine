using DefaultEcs;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.Base.EntitySystem.Time;
using MagicEngine.Engine.ECS.Core.Physics.Bridge.Components;
using MagicEngine.Engine.Base.EntityWrappers;

namespace MagicEngine.Engine.ECS.Core.Physics.Bridge;

[UpdateInBucket(ExecutionBucket.Cleanup)]
public class ResyncBodyType : EntitySystem
{
    private EntitySet? _query;

    public override void OnSceneLoad()
    {
        _query = World.GetEntities()
            .With<PhysicsBodyComponent>()
            .With<PhysicsMaterialComponent>()
            .Without<ExternallyManagedPhysicsBody>()
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
            RefreshBodyType(entity);
        }
    }

    /// <summary>
    /// Refreshes the body type for a given entity. Sets the body type of the attached physicsBody to comp.BodyType
    /// </summary>
    /// <param name="ent"></param>
    public bool RefreshBodyType(Entity ent)
    {
        if (!ent.TryGet<PhysicsBodyComponent>(out var body))
            return false;
        
        if (!ent.TryGet<PhysicsMaterialComponent>(out var mat))

        // Set the stored physicsbodys BodyType to the stored BodyType on the component
        body.Comp.Body.BodyType = mat.Comp.Type;

        return true;
    }
}