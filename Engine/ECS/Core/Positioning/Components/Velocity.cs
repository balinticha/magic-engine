using MagicEngine.Engine.Base.Debug.Attributes;
using MagicEngine.Engine.Base.PrototypeComponentSystem;
using Microsoft.Xna.Framework;

namespace MagicEngine.Engine.ECS.Core.Positioning.Components;

[Component]
struct Velocity
{
    [DataField]
    [InspectorEditable]
    public Vector2 Value;
}
