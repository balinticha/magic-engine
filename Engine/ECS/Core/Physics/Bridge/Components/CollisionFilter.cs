using System;
using MagicThing.Engine.Base.PrototypeComponentSystem;

namespace MagicThing.Engine.ECS.Core.Physics.Bridge.Components;

[Component]
public struct CollisionFilterComponent
{
    [DataField]
    public CollisionCategory Category;
    public CollisionCategory CollidesWith; // If set to `None`, it will be auto-calculated.

    public CollisionFilterComponent(CollisionCategory category, CollisionCategory collidesWith = CollisionCategory.None)
    {
        Category = category;
        CollidesWith = collidesWith;
    }
}

[Flags]
public enum CollisionCategory
{
    None = 0,
    Structure = 1, // Collides with everything except BulletShield and CounterBullet
    Shield = 2, // Collides with structures, bullets, counterBullets, and WorldStatic
    BulletShield = 4, // Collides with bullets only
    Bullet = 8, // Collides with structures, shields, bulletShields, and worldStatic
    CounterBullet = 16, // collides with bullets, BulletShields, and WorldStatic
    WorldStatic = 32, // collides with everything but bulletShields
    
    All = ~0 
}

/// <summary>
/// A static helper class that centralizes all collision filtering logic.
/// Based on a fixture's category, it determines what it should collide with.
/// </summary>
public static class CollisionRules
{
    public static CollisionCategory GetCollidesWith(CollisionCategory category)
    {
        switch (category)
        {
            case CollisionCategory.Structure:
                return CollisionCategory.All & ~CollisionCategory.BulletShield & ~CollisionCategory.CounterBullet;

            case CollisionCategory.Shield:
                return CollisionCategory.Structure | CollisionCategory.Bullet | CollisionCategory.CounterBullet | CollisionCategory.WorldStatic;

            case CollisionCategory.BulletShield:
                return CollisionCategory.Bullet;

            case CollisionCategory.Bullet:
                return CollisionCategory.Structure | CollisionCategory.Shield | CollisionCategory.BulletShield | CollisionCategory.WorldStatic;

            case CollisionCategory.CounterBullet:
                return CollisionCategory.Bullet | CollisionCategory.BulletShield | CollisionCategory.WorldStatic;

            case CollisionCategory.WorldStatic:
                return CollisionCategory.All & ~CollisionCategory.BulletShield;

            // Default case: if a category isn't defined here, it collides with everything.
            default:
                return CollisionCategory.All;
        }
    }
}