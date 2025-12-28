using MagicEngine.Engine.Base.Debug.Attributes;
using MagicEngine.Engine.Base.PrototypeComponentSystem;

namespace MagicEngine.Engine.ECS.Core.Audio.Components;

[Component]
public struct SoundListener
{
    [DataField] [InspectorSlider(0f, 1f)] public float MasterVolume;
}