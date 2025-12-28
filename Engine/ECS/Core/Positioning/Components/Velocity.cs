using MagicThing.Engine.Base.Debug.Attributes;
using MagicThing.Engine.Base.PrototypeComponentSystem;
using Microsoft.Xna.Framework;

namespace MagicThing.Engine.ECS.Core.Positioning.Components;

[Component]
struct Velocity
{
    [DataField]
    [InspectorEditable]
    public Vector2 Value;
}
