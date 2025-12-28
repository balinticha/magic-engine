using MagicThing.Engine.Base.Debug.Attributes;
using MagicThing.Engine.Base.PrototypeComponentSystem;

namespace MagicThing.Engine.ECS.Core.Physics.Behavior.Components;

[Component]
public struct Drag
{
    [DataField] [InspectorSlider(0, 1)] public float Amount;
}
