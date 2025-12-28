using MagicEngine.Engine.Base.EntitySystem;
using Microsoft.Xna.Framework;

namespace MagicEngine.Engine.ECS.Core.Physics;

[UpdateInBucket(ExecutionBucket.Update)]
public sealed class PhysicsSystem : EntitySystem
{
    /// <summary>
    /// Convert Vector2 into engine vectors
    /// </summary>
    public static Vector2 ToPhysics(Vector2 coords)
    {
        return new Vector2(
            coords.X * PhysicsConstants.MetersPerPixel,
            coords.Y * PhysicsConstants.MetersPerPixel
        );
    }

    /// <summary>
    /// Convert length to physics engine length 
    /// </summary>
    public static float ToPhysics(float value)
    {
        return value * PhysicsConstants.MetersPerPixel;
    }

    /// <summary>
    /// Convert engine vectors into Vector2
    /// </summary>
    public static Vector2 ToECS(Vector2 coords)
    {
        return new Vector2(
            coords.X * PhysicsConstants.PixelsPerMeter,
            coords.Y * PhysicsConstants.PixelsPerMeter
        );
    }
    
    /// <summary>
    /// Convert physics engine length to ECS length
    /// </summary>
    public static float ToECS(float value)
    {
        return value * PhysicsConstants.PixelsPerMeter;
    }
}