using DefaultEcs;

namespace MagicEngine.Engine.ECS.Core.Events.EntityDeath;

/// <summary>
/// The event raised before an entity dies.
/// RAISE THIS FOR: removing entities for gameplay reasons.
/// Set IsCancelled to true to prevent the action. This often requires you to fix the underlying condition.
/// </summary>
public class EntityDeathRequestEvent(Entity actor)
{
    public Entity Actor = actor;
    public bool IsCancelled = false;
}

/// <summary>
/// The event raised JUST BEFORE an entity dies, without any way of cancelling
/// </summary>
public class EntityDeathEvent(Entity actor)
{
    public Entity Actor = actor;
}

/// <summary>
/// The event raised when an entity is force killed, bypassing normal requests.
/// RAISE THIS FOR: removing entities for backend / cleanup reasons
/// If a system cannot handle this, set CancelAndRaiseFatalError to true to cause a crash.
/// </summary>
public class ForcedEntityDeathRequestEvent(Entity actor)
{
    public Entity Actor = actor;
    public bool CancelAndRaiseFatalError = false;
    public string ErrorMessage = "";
}