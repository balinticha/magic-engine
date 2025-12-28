using System;
using System.Diagnostics.CodeAnalysis;
using DefaultEcs;
using MagicThing.Engine.Base.EntitySystem;
using MagicThing.Engine.Base.EntityWrappers;
using MagicThing.Engine.Base.Scene;
using MagicThing.Engine.ECS.Core.Positioning.Components;
using MagicThing.Engine.ECS.Core.Session.Components;
using Microsoft.Xna.Framework;

namespace MagicThing.Engine.ECS.Core.Session;

[UpdateInBucket(ExecutionBucket.LateUpdate)]
public class SessionManager : EntitySystem
{
    public Vector2 ControlledEntityLastPosition;
    private Entity? _controlledEntity = null;

    public bool IsPaused = false;
    public float GameSpeed = 1.0f;

    public override void OnSceneLoad()
    {
        PersistentSceneStorage.LocalStorage storage = SceneManager.AccessStore(this);
        if (storage.Has("controlledEntity"))
        {
            _controlledEntity = storage.Get<Entity>("controlledEntity");
            LogManager.Verbose("Loaded controlled entity from persistent scene storage", "SessionManager");
        }
        
    }

    public override void OnSceneUnload()
    {
        PersistentSceneStorage.LocalStorage storage = SceneManager.AccessStore(this);
        storage.Set("controlledEntity", _controlledEntity);
        // clear the entity pointer
        _controlledEntity = null;
        LogManager.Verbose("Saved controlled entity to persistent scene storage", "SessionManager");
    }

    public bool GetControlledEntity([NotNullWhen(true)] out Entity entity)
    {
        if (_controlledEntity.HasValue && _controlledEntity.Value.IsAlive)
        {
            entity = _controlledEntity.Value;
            return true;
        };
        entity = default;
        return false;
    }

    public Entity? GetControlledEntityUnsafe()
    {
        return _controlledEntity;
    }

    public void SetControlledEntity(Entity entity)
    {
        Console.WriteLine($"[SessionManager] Attaching to entity: {entity}");
        _controlledEntity = entity;
    }

    public void SetControlledEntityIfFree(Entity entity)
    {
        if (!GetControlledEntity(out _))
            return;
        
        SetControlledEntity(entity);
    }

    public bool IsControlling(Entity entity)
    {
        if (!GetControlledEntity(out var controlledEntity))
            return false;
        
        return controlledEntity == entity;
    }

    public override void Update(Timing timing)
    {
        if (!GetControlledEntity(out var controlledEntity))
            return;
        
        if (!controlledEntity.TryGet<Position>(out var cmp))
            return;
        
        ControlledEntityLastPosition = cmp.Comp.Value;
        
    }
}

[UpdateInBucket(ExecutionBucket.First)]
public sealed class EarlyAttachSystem : EntitySystem
{
    [Dependency] private readonly SessionManager _sessionManager;

    private bool _isEnabled = true;

    public override void Update(Timing timing)
    {
        if (!_isEnabled)
        {
            return;
        }
        
        var _query = World.GetEntities()
            .With<AttachSoulOnLoad>()
            .AsSet();
        
        foreach (ref readonly var entity in _query.GetEntities())
        {
            Console.WriteLine($"[SessionManager.EarlyAttach] Early attaching triggered for: {entity}");
            _sessionManager.SetControlledEntity(entity);
            _isEnabled = false;
            return;
        }
    }

    public void Enable()
    {
        LogManager.Debug("Early Attach Enabled", "EarlyAttachSystem");
        _isEnabled = true;
    }
}