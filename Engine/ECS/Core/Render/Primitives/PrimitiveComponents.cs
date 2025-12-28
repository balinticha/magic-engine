using MagicThing.Engine.Base.Debug.Attributes;
using MagicThing.Engine.Base.PrototypeComponentSystem;
using Microsoft.Xna.Framework;

namespace MagicThing.Engine.ECS.Core.Render.Primitives;

[Component]
struct DrawRectangle
{
    [DataField] [InspectorEditable] public float Width;
    [DataField] [InspectorEditable] public float Height;
    [DataField] [InspectorEditable] public Vector2 Offset;
    [DataField] [InspectorEditable] public Color Color;
}

[Component]
struct DrawCircle
{
    [DataField] public float Radius;
    [DataField] public Vector2 Offset;
    [DataField] public Color Color;
}