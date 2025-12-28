using DefaultEcs;

namespace MagicEngine.Engine.Base.EntityWrappers;

/// <summary>
/// A pair of a DefaultECS <see cref="Entity"/> and one of its components.
/// </summary>
/// <typeparam name="T1">The type of the component.</typeparam>
public readonly struct Entity<T1>
{
    /// <summary>
    /// The entity.
    /// </summary>
    public readonly Entity Owner;

    /// <summary>
    /// The component belonging to the <see cref="Entity"/>.
    /// </summary>
    public ref T1 Comp => ref Owner.Get<T1>();

    public Entity(Entity entity)
    {
        Owner = entity;
    }
}