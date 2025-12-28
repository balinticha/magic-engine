using System.Collections.Generic;
using DefaultEcs;

namespace MagicThing.Engine.ECS.Core.Parenting.Components;

struct IsParent
{
    public List<Entity> Childrens;
}