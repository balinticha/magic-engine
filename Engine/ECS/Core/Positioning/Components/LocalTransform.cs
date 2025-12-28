using MagicEngine.Engine.Base.PrototypeComponentSystem;
using Microsoft.Xna.Framework;

namespace MagicEngine.Engine.ECS.Core.Positioning.Components;

[Component]
public struct LocalTransform
{
    [DataField]
    public Vector2 Position;
    
    [DataField]
    public float Rotation;
}