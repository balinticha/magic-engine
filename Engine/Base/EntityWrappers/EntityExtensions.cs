using DefaultEcs;
using MagicEngine.Engine.Base.PrototypeComponentSystem;

namespace MagicEngine.Engine.Base.EntityWrappers
{
    /// <summary>
    /// Provides extension methods for safely creating wrappers.
    /// </summary>
    public static class EntityWrapperExtensions
    {
        /// <summary>
        /// Tries to get the entity and its component as an Entity T1 wrapper.
        /// </summary>
        /// <returns>True if the entity has the component, otherwise false.</returns>
        public static bool TryGet<T1>(this Entity entity, out Entity<T1> result)
        {
            if (entity.Has<T1>())
            {
                result = new Entity<T1>(entity);
                return true;
            }

            result = default;
            return false;
        }

        public static bool TryRemComp<T>(this Entity entity)
        {
            if (typeof(T).IsDefined(typeof(LockedComponentAttribute), false))
            {
                return false;
            }
            
            entity.Remove<T>();
            return true;
        }
    }
}