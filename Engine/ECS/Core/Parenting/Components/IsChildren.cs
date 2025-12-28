using DefaultEcs;

namespace MagicThing.Engine.ECS.Core.Parenting.Components;

struct IsChildren
{
    public Entity Parent;
    public bool HierarchyOnly;
}