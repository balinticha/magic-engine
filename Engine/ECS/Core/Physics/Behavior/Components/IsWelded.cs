using DefaultEcs;
using nkast.Aether.Physics2D.Dynamics.Joints;

namespace MagicThing.Engine.ECS.Core.Physics.Behavior.Components;

struct IsWelded {
    public WeldJoint Joint;
    public Entity Parent;
}