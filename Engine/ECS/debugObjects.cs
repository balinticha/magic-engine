using DefaultEcs;
using MagicEngine.Engine.Base.Debug.Attributes;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.Base.EntityWrappers;
using MagicEngine.Engine.Base.PrototypeComponentSystem;
using MagicEngine.Engine.ECS.Core.Audio;
using MagicEngine.Engine.ECS.Core.Input;
using MagicEngine.Engine.ECS.Core.Physics.Behavior.Components;
using MagicEngine.Engine.ECS.Core.Physics.Bridge.Components;
using MagicEngine.Engine.ECS.Core.Positioning.Components;
using MagicEngine.Engine.ECS.Core.Session;
using Microsoft.Xna.Framework;

namespace MagicEngine.Engine.ECS;

[Component]
struct PlayerController
{
    [DataField] public float acceleration;
    [DataField] public float dashSpeed;
    [DataField] public float dashVelocityBonus;
}

// --- Systems ---

public sealed class PlayerControllerSystem : EntitySystem
{
    [Dependency] private readonly InputManagerSystem _input = null!;
    [Dependency] private readonly SessionManager _sessionManager = null!;
    [Dependency] private readonly AudioManagerSystem _audio = null!;

    private EntitySet? _query;
    
    public override void OnSceneLoad()
    {
        _query = World.GetEntities()
            .With<PlayerController>()
            .With<Position>()
            .With<Velocity>()
            .AsSet();
    }

    public override void OnSceneUnload()
    {
        _query.Dispose();
    }

    public override void Update(Timing timing)
    {
        var deltaTime = timing.DeltaTime;

        foreach (ref readonly var entity in _query.GetEntities())
        {
            if (!_sessionManager.IsControlling(entity))
                continue;
            
            ref var velocity = ref entity.Get<Velocity>();
            ref var position = ref entity.Get<Position>();
            ref readonly var cmp = ref entity.Get<PlayerController>();

            if (_input.IsPressed(InputAction.MoveUp))
            {
                velocity.Value.Y -= cmp.acceleration * deltaTime;
            }
            else if (_input.IsPressed(InputAction.MoveDown))
            {
                velocity.Value.Y += cmp.acceleration * deltaTime;
            }

            if (_input.IsPressed(InputAction.MoveRight))
            {
                velocity.Value.X += cmp.acceleration * deltaTime;
            }
            else if (_input.IsPressed(InputAction.MoveLeft))
            {
                velocity.Value.X -= cmp.acceleration * deltaTime;
            }

            if (_input.GetActionState(InputAction.SecondaryInteract) == InputState.JustPressed)
            {
                var target = new Vector2(
                    position.Value.X += velocity.Value.X * cmp.dashSpeed,
                    position.Value.Y += velocity.Value.Y * cmp.dashSpeed
                    );
                _audio.PlaySound(new PlaySoundRequest
                {
                    IsGlobal = true,
                    Pitch = 1f,
                    PitchVariance = 0.2f,
                    Priority = 0,
                    Range = 120f,
                    SoundName = "artillery",
                    Volume = 1f,
                    WorldPosition = target
                });
                entity.Set<SetPositionRequest>(new SetPositionRequest
                {
                    RequestPosition = target,
                    RequestVelocityChange = velocity.Value * cmp.dashVelocityBonus
                });
            }
        }
    }
}

[UpdateInBucket(ExecutionBucket.LateUpdate)]
public sealed class CameraFollowPlayer : EntitySystem
{
    [Dependency] private SessionManager _sessionManager = null!;

    private EntitySet? _query;

    public override void OnSceneLoad()
    {
        _query = World.GetEntities()
            .With<Position>()
            .With<Velocity>()
            .With<CameraFollowAhead>()
            .AsSet();
    }

    public override void OnSceneUnload()
    {
        _query?.Dispose();
    }

    public override void Update(Timing timing)
    {
        foreach (ref readonly var entity in _query.GetEntities())
        {
            if (!_sessionManager.IsControlling(entity))
                continue;
            
            ref var pos = ref entity.Get<Position>();
            ref var vel = ref entity.Get<Velocity>();
            ref var cfa = ref entity.Get<CameraFollowAhead>();
            
            // this is where the camera WANTS to be
            var targetPosition = pos.Value + vel.Value * cfa.Amount;
            
            // we interpolate from the CURRENT position to the TARGET position
            Camera.Position = Vector2.Lerp(
                Camera.Position,
                targetPosition,
                cfa.Interpolation * timing.DeltaTime * 10f
            );
        }
    }
}

[Component]
public struct CameraFollowAhead
{
    [DataField] [InspectorSlider(0f, 1.2f)] public float Amount;
    [DataField] [InspectorSlider(0.01f, 1f)] public float Interpolation;
}

public sealed class DragSystem : EntitySystem
{
    private EntitySet? _query;

    public override void OnSceneLoad()
    {
        _query = World.GetEntities().With<Drag>().With<Velocity>().AsSet();
    }

    public override void OnSceneUnload()
    {
        _query?.Dispose();
    }

    public override void Update(Timing timing)
    {
        foreach (ref readonly var entity in _query.GetEntities())
        {
            ref var cmp = ref entity.Get<Velocity>();
            var drag = entity.Get<Drag>().Amount;
            
            cmp.Value *= 1 - drag;
        }
    }
}

[Component]
public struct DelOnCollision;

public sealed class DeleteOnCollision : EntitySystem
{
    [Dependency] private EntityOperationHelpers _entop = null!;

    public override void OnSceneLoad()
    {
        Events.Subscribe<DelOnCollision, CollisionEvent>(OnCollision);
    }

    public override void OnSceneUnload()
    {
        Events.Subscribe<DelOnCollision, CollisionEvent>(OnCollision);
    }

    private void OnCollision(Entity<DelOnCollision> ent, CollisionEvent ev)
    {
        _entop.TryKillEntity(ent.Owner);
    }
}