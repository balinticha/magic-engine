using MagicThing.Engine.Base.PrototypeComponentSystem;
using Microsoft.Xna.Framework;

namespace MagicThing.Engine.ECS.Core.Positioning.Components;

[Component]
public struct LocalTransform
{
    [DataField]
    public Vector2 Position;
    
    [DataField]
    public float Rotation;
}