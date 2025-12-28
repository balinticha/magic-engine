using MagicEngine.Engine.Base.Debug.Attributes;
using MagicEngine.Engine.Base.PrototypeComponentSystem;

namespace MagicEngine.Engine.ECS.Core.Physics.Behavior.Components;

[Component]
public struct Drag
{
    [DataField] [InspectorSlider(0, 1)] public float Amount;
}
