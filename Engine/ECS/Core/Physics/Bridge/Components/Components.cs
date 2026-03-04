using MagicEngine.Engine.Base.Debug.Attributes;
using MagicEngine.Engine.Base.PrototypeComponentSystem;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Dynamics;

namespace MagicEngine.Engine.ECS.Core.Physics.Bridge.Components;

// TODO make these serializable
/// <summary>
/// Internal component
/// </summary>
public struct PhysicsBodyComponent()
{
    public Body Body;
}

/// <summary>
/// Defines the physical material properties of a body's fixtures.
/// </summary>
[Component]
public struct PhysicsMaterialComponent()
{
    [DataField] [InspectorEditable] public float Density;
    [DataField] public BodyType Type = BodyType.Dynamic;
    // [DataField] public float Friction;
    // [DataField] public float Restitution; // Bounciness
}

[Component]
public struct RectangleColliderComponent
{
    [DataField] public float Width;
    [DataField] public float Height;
    [DataField] public Vector2 Offset;
    [DataField] public bool IsSensor;
}