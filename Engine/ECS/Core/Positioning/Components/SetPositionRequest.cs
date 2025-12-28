using Microsoft.Xna.Framework;

namespace MagicThing.Engine.ECS.Core.Positioning.Components;

struct SetPositionRequest
{
    public Vector2 RequestPosition;
    public Vector2 RequestVelocityChange;
}