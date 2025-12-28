namespace MagicThing.Engine.ECS.Core.Physics;

public static class PhysicsConstants
{
    /// <summary>
    /// The number of pixels that represent one meter in the physics simulation.
    /// </summary>
    public const float PixelsPerMeter = 100f;

    /// <summary>
    /// The number of meters that represent one pixel.
    /// This is the value used to convert from display units to simulation units.
    /// </summary>
    public const float MetersPerPixel = 1f / PixelsPerMeter;
}