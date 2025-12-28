using DefaultEcs;
using nkast.Aether.Physics2D.Dynamics.Contacts;

namespace MagicEngine.Engine.ECS.Core.Physics.Bridge.Components;

public class CollisionEvent(in Entity self, in Entity other, Contact contact)
{
    public readonly Entity Self = self;
    public readonly Entity Other = other;
    public readonly Contact Contact = contact;
}