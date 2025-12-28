using System;
using DefaultEcs;
using MagicEngine.Engine.Base.Debug;
using MagicEngine.Engine.Base.Events;
using MagicEngine.Engine.Base.PrototypeComponentSystem;
using MagicEngine.Engine.Base.Scene;
using MagicEngine.Engine.ECS.Core.Camera;
using MagicEngine.Engine.ECS.Core.Events;
using Microsoft.Xna.Framework.Graphics;

namespace MagicEngine.Engine.Base.EntitySystem;

public abstract class EntitySystem
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
    public Microsoft.Xna.Framework.Content.ContentManager Content { get; internal set; } = null!;
    
    
    
    /// <summary>
    /// The method called ONCE on entity system initialization. Do not perform scene-specific operations here, like subscribing to event listeners
    /// </summary>
    public virtual void Initialize() { }
    
    /// <summary>
    /// The method called when a scene is loaded. Subscribe to event listeners and perform other scene-specific setup here
    /// </summary>
    public virtual void OnSceneLoad() { }
    
    /// <summary>
    /// The method called when a scene is unloaded. Perform cleanup for anything you've done in OnSceneLoad().
    /// </summary>
    public virtual void OnSceneUnload() { }
    public virtual void Update(Timing timing) { }
    
    /// <summary>
    /// Called for systems in the Render bucket during the main Draw call.
    /// </summary>
    public virtual void Draw(Timing timing, SpriteBatch spriteBatch, Microsoft.Xna.Framework.Matrix transformMatrix) { }
    
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

    protected void Vx(string text)
    {
        LogManager.Log(text, GetType().Name, LogLevel.VerboseExtra);
    }

    protected void AssertFailure(string text, LogLevel level = LogLevel.Debug)
    {
        LogManager.AssertFailure(text, GetType().Name, level);
    }
}
