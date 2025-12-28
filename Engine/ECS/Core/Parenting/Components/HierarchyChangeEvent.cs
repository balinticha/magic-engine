using DefaultEcs;

namespace MagicEngine.Engine.ECS.Core.Parenting.Components;

public struct HierarchyChangeEvent()
{
    public HierarchyChangeEventType Type;
    public Entity Actor;
    public Entity Parent;
}

public enum HierarchyChangeEventType
{
    Attached = 0,
    Detached = 1,
}