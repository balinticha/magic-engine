using MagicThing.Engine.Base.Debug.Attributes;
using MagicThing.Engine.Base.PrototypeComponentSystem;

namespace MagicThing.Engine.ECS.Core.Audio.Components;

[Component]
public struct SoundListener
{
    [DataField] [InspectorSlider(0f, 1f)] public float MasterVolume;
}