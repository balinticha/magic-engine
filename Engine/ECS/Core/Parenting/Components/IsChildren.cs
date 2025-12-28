using DefaultEcs;

namespace MagicEngine.Engine.ECS.Core.Parenting.Components;

struct IsChildren
{
    public Entity Parent;
    public bool HierarchyOnly;
}