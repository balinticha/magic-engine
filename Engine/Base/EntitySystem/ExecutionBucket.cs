using System;

namespace MagicEngine.Engine.Base.EntitySystem;

/// <summary>
/// Defines the different execution "buckets" or stages within the game loop.
/// Systems are placed into these buckets to control their execution order.
/// </summary>
public enum ExecutionBucket
{
    // --- Runs at the start of the variable-time MonoGame Update() ---
    First,
    Input,

    // --- Runs in the fixed-time update loop ---
    PreUpdate,
    Update,         // Default bucket for most game logic systems
    PostPhysics,

    // --- Runs after the fixed-time loop in the variable-time MonoGame Update() ---
    LateUpdate,     // Ideal for camera updates that track physics objects

    // --- Runs at the very end of the MonoGame Update() to prepare for the next frame ---
    Cleanup,
    PreRender,
    
    // --- Runs inside the MonoGame Draw() method ---
    Audio,  // Runs for audio-related things before anything is drawn to the screen, once per frame
    Render,
    
    // --- Runs before Render ONLY when paused
    UpdatePaused,
    
    // --- Runs before UpdatePaused when paused, runs after First when unpaused.,
    Transient
}

/// <summary>
/// Place this attribute on an EntitySystem class to specify which ExecutionBucket it
/// should run in. If omitted, the system will default to the 'Update' bucket.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UpdateInBucketAttribute : Attribute
{
    public ExecutionBucket Bucket { get; }

    public UpdateInBucketAttribute(ExecutionBucket bucket)
    {
        Bucket = bucket;
    }
}