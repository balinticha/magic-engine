using MagicThing.Engine.Base.Debug.Attributes;
using MagicThing.Engine.Base.PrototypeComponentSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MagicThing.Engine.ECS.Core.Render.Components;

[Component]
struct Sprite()
{
    [DataField]
    public Texture2D Texture;
    [DataField] [InspectorEditable]
    public Color Color;
    
    [DataField] [InspectorEditable]
    public int Layer;
    
    [DataField] [InspectorEditable]
    public float SortOffset;
    
    [DataField] [InspectorSlider(0, 15)]
    public float Intensity = 1f;
}