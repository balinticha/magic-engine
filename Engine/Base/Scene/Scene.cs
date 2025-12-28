using System;
using System.Collections.Generic;
using DefaultEcs;
using DefaultEcs.System;
using MagicEngine.Engine.Base.Debug.UI;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.Base.Events;
using MagicEngine.Engine.ECS.Core.Physics.Bridge;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Diagnostics;

namespace MagicEngine.Engine.Base.Scene;

public struct Scene(SceneCreationResources creationResources, string name, World world, EventManager eventManager, nkast.Aether.Physics2D.Dynamics.World physicsWorld)
{
    public readonly string Name = name;
    public readonly World EcsWorld = world;
    public readonly EventManager EventManager = eventManager;
    public nkast.Aether.Physics2D.Dynamics.World PhysicsWorld = physicsWorld;
    public AttachedSceneSystems AttachedSystems = new AttachedSceneSystems(world, physicsWorld, eventManager, name, creationResources);
    public PersistentSceneStorage PersistentSceneStorage = new PersistentSceneStorage();
}

public class PersistentSceneStorage
{
    private readonly Dictionary<string, Dictionary<string, object>> _storage = new();

    public LocalStorage Access(object owner)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        
        string classKey = owner.GetType().FullName;
        
        if (!_storage.ContainsKey(classKey))
        {
            _storage[classKey] = new Dictionary<string, object>();
        }
        
        return new LocalStorage(_storage[classKey]);
    }

    public readonly struct LocalStorage
    {
        private readonly Dictionary<string, object> _storage;

        public LocalStorage(Dictionary<string, object> storage)
        {
            _storage = storage;
        }

        public void Set<T>(string key, T value)
        {
            _storage[key] = value;
        }

        public T Get<T>(string key)
        {
            if (_storage.TryGetValue(key, out object value))
            {
                if (value is T castValue)
                {
                    return castValue;
                }
                
                throw new InvalidCastException($"Cannot cast stored value of type {value.GetType().FullName} to requested type {typeof(T).FullName} for key '{key}'");
            }

            return default;
        }
        
        public bool Has(string key) => _storage.ContainsKey(key);
    }
}

public class AttachedSceneSystems
{
    #region DefaultECS systems
    public PhysicsBodyDeletionSystem BodyDeletionSystem;
    public PhysicsBodyCreationSystem BodyCreationSystem;
    public PrePhysicsSyncSystem PrePhysicsSyncSystem;
    public PostPhysicsSyncSystem PostPhysicsSyncSystem;
    #endregion
    
    #region UI related systems
    public DebugView DebugView;
    public SceneGraphPanel SceneGraphPanel;
    #endregion

    public AttachedSceneSystems(World world, nkast.Aether.Physics2D.Dynamics.World pworld, EventManager evm,
        string sceneName, SceneCreationResources scr)
    {
        BodyDeletionSystem = new PhysicsBodyDeletionSystem(world, pworld);
        BodyCreationSystem = new PhysicsBodyCreationSystem(world, pworld);
        PrePhysicsSyncSystem = new PrePhysicsSyncSystem(world);
        PostPhysicsSyncSystem = new PostPhysicsSyncSystem(world, pworld, evm);
        
        DebugView = new DebugView(pworld);
        SceneGraphPanel = new SceneGraphPanel(world, sceneName);
        
        LoadContent(scr);
    }
    
    private void LoadContent(SceneCreationResources scr)
    {
        DebugView.LoadContent(scr.GraphicsDevice, scr.Content);
    }
    
}

public struct SceneCreationResources(GraphicsDevice graphicsDevice, ContentManager content)
{
    public readonly GraphicsDevice GraphicsDevice = graphicsDevice;
    public readonly ContentManager Content = content;
}

public class SceneManager(GraphicsDevice graphicsDevice, ContentManager content)
{
    internal SystemManager _systemManager = null!;
    
    protected Dictionary<string, Scene> Scenes  = new Dictionary<string, Scene>();
    protected string CurrentScene = "EngineNoSceneSet"; // properly overwritten at init. Otherwise it'd crash anyways.

    public bool SceneExists(string sceneName)
    {
        return Scenes.ContainsKey(sceneName);
    }
    
    public void RegsiterScene(Scene scene)
    {
        Console.WriteLine($"[SceneManager] Registering scene '{scene.Name}'");
        Scenes.Add(scene.Name, scene);
    }

    public bool TryDeregisterScene(string sceneName)
    {
        if (sceneName == CurrentScene)
        {
            return false;
        }
        
        Console.WriteLine($"[SceneManager] Deregistering scene '{CurrentScene}'");
        Scenes.Remove(sceneName);
        return true;
    }

    public Scene GetScene(string name)
    {
        return Scenes[name];
    }

    public Scene GetScene()
    {
        return Scenes[CurrentScene];
    }

    public PersistentSceneStorage.LocalStorage AccessStore(object owner)
    {
        return Scenes[CurrentScene].PersistentSceneStorage.Access(owner);
    }

    public Dictionary<string, Scene>.KeyCollection GetSceneNames()
    {
        return Scenes.Keys;
    }

    public bool TrySetActive(string sceneName)
    {
        // Ensure the scene exists first!
        if (!Scenes.ContainsKey(sceneName))
        {
            return false;
        }
        
        Console.WriteLine($"[SceneManager] Activating scene '{sceneName}'");
        _systemManager.CallOnSceneUnload();
        CurrentScene = sceneName;
        _systemManager.CallOnSceneLoad();
        return true;
    }

    public void FirstLoadSceneUnsafe(string sceneName)
    {
        CurrentScene = sceneName;
        _systemManager.CallOnSceneLoad();
    }
}