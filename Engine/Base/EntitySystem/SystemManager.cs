using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MagicEngine.Engine.Base.Debug;
using MagicEngine.Engine.Base.PrototypeComponentSystem;
using MagicEngine.Engine.Base.Scene;
using MagicEngine.Engine.ECS.Core.Camera;
using MagicEngine.Engine.Base.Events;
using Microsoft.Xna.Framework.Graphics;

namespace MagicEngine.Engine.Base.EntitySystem;

public class SystemManager(SceneManager sceneManager, Random random, PrototypeManager prm, CameraSystem cs, LogManager lm, Microsoft.Xna.Framework.Content.ContentManager content)
{
    private readonly SceneManager _sceneManager = sceneManager;
    private readonly PrototypeManager _prototypeManager = prm;
    private readonly Random _random = random;
    private readonly CameraSystem _cameraSystem = cs;
    private readonly LogManager _logManager = lm;
    private readonly Microsoft.Xna.Framework.Content.ContentManager _content = content;
    
    public SystemProfiler Profiler = new();
    
    // This dictionary holds all of our system instances, mapped by their Type for fast lookup
    private readonly Dictionary<Type, EntitySystem> _systemsByType = new();
    
    // Stores lists of systems, organized by the bucket they should run in.
    private readonly Dictionary<ExecutionBucket, List<EntitySystem>> _systemsByBucket = new();
    
    private readonly List<EntitySystem> _systemsToDeregister = new();

    public void Initialize()
    {
        _sceneManager._systemManager = this;
        
        // Initialize the bucket dictionary to ensure all buckets have a list.
        foreach (ExecutionBucket bucket in Enum.GetValues(typeof(ExecutionBucket)))
        {
            _systemsByBucket[bucket] = new List<EntitySystem>();
        }
        
        // --- Part 1: Discovery and Instantiation ---
        // Find all types in our program that inherit from EntitySystem.
        var systemTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsSubclassOf(typeof(EntitySystem)) && !t.IsAbstract);

        foreach (var type in systemTypes)
        {
            var systemInstance = (EntitySystem)Activator.CreateInstance(type)!;
            systemInstance.SceneManager = _sceneManager;
            systemInstance.Random = _random;
            systemInstance.Prototypes = _prototypeManager;
            systemInstance.Camera = _cameraSystem;
            systemInstance.SystemManager = this;
            systemInstance.LogManager = _logManager;
            systemInstance.Content = _content;
            
            _systemsByType.Add(type, systemInstance);

            var bucketAttribute = type.GetCustomAttribute<UpdateInBucketAttribute>();
            var bucket = bucketAttribute?.Bucket ?? ExecutionBucket.Update; // Default to 'Update' if no attribute is found

            // Add it to the correct execution bucket
            _systemsByBucket[bucket].Add(systemInstance);

            Console.WriteLine($"[SystemManager] Created system: {type.Name} in bucket: {bucket}");
        }

        // --- Part 2: Dependency Injection ---
        // Now that all systems are created, we can inject dependencies.
        foreach (var system in _systemsByType.Values)
        {
            // Find all fields in this system marked with [Dependency]
            var dependencyFields = system.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.IsDefined(typeof(DependencyAttribute), false));
            
            foreach (var field in dependencyFields)
            {
                // Find the required system instance from our dictionary.
                if (_systemsByType.TryGetValue(field.FieldType, out var dependencyInstance))
                {
                    // Inject it!
                    field.SetValue(system, dependencyInstance);
                    Console.WriteLine($"Injected {field.FieldType.Name} into {system.GetType().Name}");
                }
                else
                {
                    throw new Exception($"Failed to resolve dependency: {field.FieldType.Name} for system: {system.GetType().Name}");
                }
            }
        }
        
        // --- Part 3: Final Initialization ---
        // Now that all dependencies are injected, call Initialize() on each system.
        foreach (var system in _systemsByType.Values)
        {
            system.Initialize();
        }
        
        Console.WriteLine("[SystemManager] All systems initialized.");
    }

    /// <summary>
    /// Runs all systems within a specific execution bucket.
    /// </summary>
    private void RunBucket(ExecutionBucket bucket, Timing timing)
    {
        foreach (var system in _systemsByBucket[bucket])
        {
            Profiler.Profile(system.GetType().Name, () => system.Update(timing));
        }
    }
    
    /// <summary>
    /// Queues a system to be deregistered at the end of the current frame.
    /// Its Update/Draw methods will no longer be called.
    /// </summary>
    public void DeregisterSystem(EntitySystem system)
    {
        // Add to the queue if it's not already there
        if (!_systemsToDeregister.Contains(system))
        {
            _systemsToDeregister.Add(system);
            Console.WriteLine($"[SystemManager] Queued {system.GetType().Name} for deregistration.");
        }
    }
    
    private void ProcessDeregistrations()
    {
        if (_systemsToDeregister.Count == 0)
        {
            return;
        }

        foreach (var systemToRemove in _systemsToDeregister)
        {
            // Remove from the master type dictionary
            _systemsByType.Remove(systemToRemove.GetType());
            
            // Remove from profiler tracking
            Profiler.Remove(systemToRemove.GetType().Name);

            // Find and remove it from its execution bucket list
            foreach (var bucketList in _systemsByBucket.Values)
            {
                // Remove returns true if the item was found and removed
                if (bucketList.Remove(systemToRemove))
                {
                    Console.WriteLine($"[SystemManager] Deregistered {systemToRemove.GetType().Name}.");
                    break; // Assume a system is only in one bucket
                }
            }
        }

        // Clear the queue for the next frame
        _systemsToDeregister.Clear();
    }
    
    /// <summary>
    /// Retrieves a registered system of a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the EntitySystem to retrieve.</typeparam>
    /// <returns>The system instance</returns>
    public T GetSystem<T>() where T : EntitySystem
    {
        if (_systemsByType.TryGetValue(typeof(T), out var system))
        {
            return (T)system;
        }
        
        throw new Exception($"System of type {typeof(T).Name} not found.");
    }
    
    public void CallOnSceneLoad()
    {
        _logManager.Log("Calling OnSceneLoad on all systems", "SystemManager", LogLevel.VerboseExtra);
        
        foreach (var system in _systemsByType.Values)
        {
            system.OnSceneLoad();
        }
    }
    
    public void CallOnSceneUnload()
    {
        _logManager.Log("Calling OnSceneUnload on all systems", "SystemManager", LogLevel.VerboseExtra);
        
        foreach (var system in _systemsByType.Values)
        {
            system.OnSceneUnload();
        }
    }
    
    #region Game Loop Execution Methods

    public void RunFrameStart(Timing timing)
    {
        RunBucket(ExecutionBucket.First, timing);
        RunBucket(ExecutionBucket.Input, timing);
    }

    public void RunFixedUpdatePrePhysics(Timing timing)
    {
        RunBucket(ExecutionBucket.PreUpdate, timing);
        RunBucket(ExecutionBucket.Update, timing);
        
    }

    public void RunFixedUpdatePostPhysics(Timing timing)
    {
        RunBucket(ExecutionBucket.PostPhysics, timing);
    }

    public void RunFrameLateUpdate(Timing timing)
    {
        RunBucket(ExecutionBucket.LateUpdate, timing);
    }
        
    public void RunDraw(Timing timing, SpriteBatch spriteBatch, Microsoft.Xna.Framework.Matrix transformMatrix)
    {
        Profiler.Profile("Bucket: Audio", () => RunBucket(ExecutionBucket.Audio, timing));

        foreach (var system in _systemsByBucket[ExecutionBucket.Render])
        {
            Profiler.Profile(system.GetType().Name, () => system.Draw(timing, spriteBatch, transformMatrix));
        }
    }

    public void RunFrameEnd(Timing timing)
    {
        RunBucket(ExecutionBucket.Cleanup, timing);
        RunBucket(ExecutionBucket.PreRender, timing);
        ProcessDeregistrations();
    }

    public void RunPausedUpdate(Timing timing)
    {
        RunBucket(ExecutionBucket.UpdatePaused, timing);
    }

    public void RunTransientUpdate(Timing timing)
    {
        RunBucket(ExecutionBucket.Transient, timing);
    }
    #endregion
}