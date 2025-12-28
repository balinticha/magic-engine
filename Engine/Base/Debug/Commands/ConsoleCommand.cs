using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DefaultEcs;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.Base.Events;
using MagicEngine.Engine.Base.PrototypeComponentSystem;
using MagicEngine.Engine.Base.Scene;
using MagicEngine.Engine.Base.Shaders.PostProcessing;
using MagicEngine.Engine.ECS.Core.Camera;

namespace MagicEngine.Engine.Base.Debug.Commands;

/// <summary>
/// A base class for commands to inherit from, providing easy access to core systems.
/// </summary>
public abstract class ConsoleCommand : IConsoleCommand
{
    public SceneManager SceneManager { get; internal set; } = null!;
    public World World => SceneManager.GetScene().EcsWorld;
    public nkast.Aether.Physics2D.Dynamics.World PhysicsWorld => SceneManager.GetScene().PhysicsWorld;
    public EventManager Events => SceneManager.GetScene().EventManager;
    public PrototypeManager Prototypes { get; internal set; } = null!;
    public Random Random { get; internal set; } = null!;
    public SystemManager SystemManager { get; internal set; } = null!;
    public CameraSystem Camera { get; internal set; } = null!;
    public LogManager LogManager { get; internal set; } = null!;
    public PostProcessingManager PostProcessingManager { get; internal set; } = null!;
    
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual bool AllowCrashes { get; } = false;
    public abstract string Execute(string[] args);
    
    /// <summary>
    /// Finds an entity by its debug ID string (e.g., "1:1.0").
    /// </summary>
    /// <param name="id">The string ID of the entity to find.</param>
    /// <param name="entity">The found entity, if successful.</param>
    /// <returns>True if an entity was found, otherwise false.</returns>
    protected bool TryGetEntityById(string id, out Entity entity)
    {
        // VERY slow. O(n) time. Meh.
        entity = World.FirstOrDefault(e => {
            var parts = e.ToString().Split(' ');
            return parts.Length == 2 && parts[1] == id;
        });
        
        return entity.IsAlive;
    }
    
    protected bool TryFindComponentType(string componentName, out Type? componentType)
    {
        componentType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .FirstOrDefault(type => string.Equals(type.Name, componentName, StringComparison.OrdinalIgnoreCase));
        
        return componentType != null;
    }

    protected bool HasComponentByType(in Entity entity, Type componentType)
    {
        // Get the generic Has<T>() method
        MethodInfo? hasMethod = typeof(Entity).GetMethod(nameof(Entity.Has));
        if (hasMethod == null) return false;

        // Create a specific method, e.g., Has<Position>()
        MethodInfo genericHasMethod = hasMethod.MakeGenericMethod(componentType);

        // Invoke the method and cast the result to a bool
        return (bool)genericHasMethod.Invoke(entity, null)!;
    }

    protected void RemoveComponentByType(in Entity entity, Type componentType)
    {
        // Get the generic Remove<T>() method
        MethodInfo? removeMethod = typeof(Entity).GetMethod(nameof(Entity.Remove));
        if (removeMethod == null) return;

        // Create a specific method, e.g., Remove<Position>()
        MethodInfo genericRemoveMethod = removeMethod.MakeGenericMethod(componentType);
        
        // Invoke the method
        genericRemoveMethod.Invoke(entity, null);
    }
    
    protected void SetComponentByType(in Entity entity, Type componentType, object componentInstance)
    {
        // Get all methods named "Set", then find the specific overload we need.
        MethodInfo? setMethod = typeof(Entity).GetMethods()
            .FirstOrDefault(m =>
                // 1. It must be named "Set".
                m.Name == nameof(Entity.Set) &&
                // 2. It must be a generic method definition.
                m.IsGenericMethodDefinition &&
                // 3. It must have exactly ONE parameter (this is the key to resolving the ambiguity).
                m.GetParameters().Length == 1); 
            
        if (setMethod == null)
        {
            // This should NEVER ever ever ever happen but who the fuck knows at this point
            // it might be a fucking lie
            Console.WriteLine("Error: Could not find the appropriate Set<T>(T) method on the Entity type.");
            return;
        }

        // Create a specific method, e.g., Set<Position>()
        MethodInfo genericSetMethod = setMethod.MakeGenericMethod(componentType);

        // Invoke the method, passing the component instance as a parameter
        genericSetMethod.Invoke(entity, new object[] { componentInstance });
    }
    
    /// <summary>
    /// Safely parses a numeric value from a string argument.
    /// </summary>
    /// <typeparam name="T">The numeric type to parse into (e.g., int, float, double).</typeparam>
    /// <param name="arg">The string argument to parse.</param>
    /// <param name="result">The parsed numeric value, if successful.</param>
    /// <returns>True if the string was successfully parsed, otherwise false.</returns>
    protected bool TryParseNumber<T>(string arg, out T result) where T : IConvertible
    {
        result = default;
        if (string.IsNullOrWhiteSpace(arg))
        {
            return false;
        }

        try
        {
            // Use CultureInfo.InvariantCulture to ensure '.' is always the decimal separator.
            var convertedValue = Convert.ChangeType(arg, typeof(T), CultureInfo.InvariantCulture);
            if (convertedValue != null)
            {
                result = (T)convertedValue;
                return true;
            }
        }
        catch (FormatException)
        {
            // The string was not in a valid format.
            return false;
        }
        catch (OverflowException)
        {
            // The number was too large or too small for the target type.
            return false;
        }

        return false;
    }
    
    // Logging wrapper methods
    protected void Log(string text, LogLevel level = LogLevel.Debug)
    {
        LogManager.Log(text, GetType().Name, level);
    }
    
    protected void D(string text)
    {
        LogManager.Debug(text, GetType().Name);
    }
    
    protected void R(string text)
    {
        LogManager.Release(text, GetType().Name);
    }
    
    protected void V(string text)
    {
        LogManager.Verbose(text, GetType().Name);
    }
}


[AttributeUsage(AttributeTargets.Class)]
public class BuiltInCommandAttribute : Attribute { }