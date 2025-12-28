using System.Collections.Generic;
using DefaultEcs;

namespace MagicEngine.Engine.ECS.Core.Parenting.Components;

struct IsParent
{
    public List<Entity> Childrens;
}