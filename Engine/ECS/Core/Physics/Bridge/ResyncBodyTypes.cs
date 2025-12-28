using DefaultEcs;
using MagicThing.Engine.Base.EntitySystem;
using MagicThing.Engine.Base.EntityWrappers;
using MagicThing.Engine.ECS.Core.Physics.Bridge.Components;

namespace MagicThing.Engine.ECS.Core.Physics.Bridge;

[UpdateInBucket(ExecutionBucket.Cleanup)]
public class ResyncBodyType : EntitySystem
{
    public override void Update(Timing timing)
    {
        var _query = World.GetEntities()
            .With<PhysicsBodyComponent>()
            .AsSet();
        
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
        
        // Set the stored physicsbodys BodyType to the stored BodyType on the component
        body.Comp.Body.BodyType = body.Comp.Type;

        return true;
    }
}