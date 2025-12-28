using MagicThing.Engine.Base.Debug.Attributes;
using MagicThing.Engine.Base.PrototypeComponentSystem;
using Microsoft.Xna.Framework;

namespace MagicThing.Engine.ECS.Core.Positioning.Components;

[Component]
public struct Position
{
    [DataField]
    public Vector2 Value;
    
    [DataField] [InspectorSlider(0, 10)]
    public float Rotation;
}

/// <summary>
/// A component, that when added, enables interpolation.
/// </summary>
[Component]
public struct PreviousPosition
{
    public Vector2 Value;
    public float Rotation;
}

/// <summary>
/// A component that is automatically added to any object with Position
/// </summary>
[Component]
public struct RenderPosition
{
    public Vector2 Value;
    public float Rotation;
}