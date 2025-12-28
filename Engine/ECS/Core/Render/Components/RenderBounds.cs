using MagicThing.Engine.Base.Debug.Attributes;
using MagicThing.Engine.Base.PrototypeComponentSystem;
using Microsoft.Xna.Framework;

namespace MagicThing.Engine.ECS.Core.Render.Components;

[Component]
public struct RenderBounds()
{
    [DataField] [InspectorEditable] public float Width;

    [DataField] [InspectorEditable] public float Height;

    [DataField] [InspectorEditable] public Vector2 Anchor = new Vector2(0.5f, 0.5f); // 0,0 = top left, 0.5,0.5 = center

    [DataField] [InspectorEditable] public int Layer;

    [DataField] [InspectorEditable] public float SortOffset;
}
