using MagicThing.Engine.Base.Debug.Attributes;
using MagicThing.Engine.Base.PrototypeComponentSystem;
using Microsoft.Xna.Framework.Audio;

namespace MagicThing.Engine.ECS.Core.Audio.Components;

#nullable enable

[Component]
public struct ContinousSoundEmitter
{
    [DataField] public string Name;
    [DataField] [InspectorSlider(0f, 1f)] public float Volume;
    [DataField] [InspectorSlider(-1f, 1f)] public float Pitch;
    [DataField] [InspectorEditable] public float Range;
    [DataField] public bool Loop;
    [DataField] public bool IsPositional;
    [DataField] public int Importance;

    internal SoundEffectInstance? ActiveInstance;
}