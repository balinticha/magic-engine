using DefaultEcs;
using MagicThing.Engine.Base.Debug.Attributes;
using MagicThing.Engine.Base.EntitySystem;
using MagicThing.Engine.Base.EntityWrappers;
using MagicThing.Engine.Base.PrototypeComponentSystem;
using MagicThing.Engine.ECS.Core.Audio;
using MagicThing.Engine.ECS.Core.Input;
using MagicThing.Engine.ECS.Core.Physics.Behavior.Components;
using MagicThing.Engine.ECS.Core.Physics.Bridge.Components;
using MagicThing.Engine.ECS.Core.Positioning.Components;
using MagicThing.Engine.ECS.Core.Session;
using Microsoft.Xna.Framework;

namespace MagicThing.Engine.ECS;

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

    public override void Update(Timing timing)
    {
        var _query = World.GetEntities()
            .With<PlayerController>()
            .With<Position>()
            .With<Velocity>()
            .AsSet();
        
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

    public override void Update(Timing timing)
    {
        var _query = World.GetEntities()
            .With<Position>()
            .With<Velocity>()
            .With<CameraFollowAhead>()
            .AsSet();
        
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
    public override void Update(Timing timing)
    {
        var _query = World.GetEntities().With<Drag>().With<Velocity>().AsSet();
        
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