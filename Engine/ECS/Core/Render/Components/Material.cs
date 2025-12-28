using MagicEngine.Engine.Base.Debug.Attributes;
using MagicEngine.Engine.Base.PrototypeComponentSystem;
using Microsoft.Xna.Framework.Graphics;

namespace MagicEngine.Engine.ECS.Core.Render.Components;

[Component]
public struct Material()
{
    [DataField]
    public Effect Effect;

    private System.Collections.Generic.Dictionary<string, object> _parameters;

    private int CachedHash;
    public int GetCachedHash => CachedHash;

    [DataField]
    public System.Collections.Generic.Dictionary<string, object> Parameters
    {
        get => _parameters;
        set
        {
            _parameters = value;
            UpdateHash();
        }
    }

    [DataField]
    public MaterialBlendMode BlendMode = MaterialBlendMode.AlphaBlend;

    [DataField] [InspectorSlider(0, 15)] 
    public float Intensity = 1f;

    public void SetParameter(string name, object value)
    {
        if (_parameters == null)
        {
            _parameters = new System.Collections.Generic.Dictionary<string, object>();
        }
        _parameters[name] = value;
        UpdateHash();
    }

    public void ForcedUpdateHash()
    {
        UpdateHash();
    }

    private void UpdateHash()
    {
        CachedHash = 0;
        if (_parameters == null) return;
        
        foreach (var kvp in _parameters)
        {
            int entryHash = kvp.Key.GetHashCode();
            // ContentManager should return the same Texture2D instance for 
            // the same file path, so hashing that won't break batching.
            if (kvp.Value != null)
                entryHash = (entryHash * 397) ^ kvp.Value.GetHashCode();
            
            // collision risk is real, but chance is 1 in 4,294,967,296
            // for a renderer where a collision is just a minor visual glitch
            // and always rendering less than 10k objets, this is an acceptable tradeoff.
            CachedHash ^= entryHash;
        }
    }
}
