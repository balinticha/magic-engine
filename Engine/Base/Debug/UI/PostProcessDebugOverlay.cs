using System;
using System.Linq;
using System.Reflection;
using ImGuiNET;
using MagicThing.Engine.Base.Shaders.PostProcessing;
using Microsoft.Xna.Framework;

namespace MagicThing.Engine.Base.Debug.UI;

public class PostProcessDebugOverlay
{
    private readonly PostProcessingManager _postProcessingManager;

    public PostProcessDebugOverlay(PostProcessingManager postProcessingManager)
    {
        _postProcessingManager = postProcessingManager;
    }

    public void Draw()
    {
        if (!_postProcessingManager.IsDebugMenuOpen)
            return;

        bool isOpen = true;
        if (ImGui.Begin("Post Processing Config", ref isOpen))
        {
            if (!isOpen)
            {
                _postProcessingManager.IsDebugMenuOpen = false;
                ImGui.End();
                return;
            }

            ImGui.Text($"Total Effects: {_postProcessingManager.Effects.Count}");
            ImGui.Separator();

            for (int i = 0; i < _postProcessingManager.Effects.Count; i++)
            {
                var effect = _postProcessingManager.Effects[i];
                if (ImGui.CollapsingHeader($"[{i}] {effect.GetType().Name}##{i}", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    bool enabled = effect.Enabled;
                    if (ImGui.Checkbox($"Enabled##{i}", ref enabled))
                    {
                        effect.Enabled = enabled;
                    }
                    
                    // Expose parameters via Reflection
                    // Very ineffective, very uncached, very idontcare
                    var properties = effect.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    
                    foreach (var prop in properties)
                    {
                        if (!prop.CanRead || !prop.CanWrite) continue;

                        string label = $"{prop.Name}##{i}";
                        
                        if (prop.PropertyType == typeof(float))
                        {
                            float val = (float)prop.GetValue(effect)!;
                            if (ImGui.DragFloat(label, ref val, 0.01f))
                            {
                                prop.SetValue(effect, val);
                            }
                        }
                        else if (prop.PropertyType == typeof(int))
                        {
                            int val = (int)prop.GetValue(effect)!;
                            if (ImGui.DragInt(label, ref val, 1))
                            {
                                prop.SetValue(effect, val);
                            }
                        }
                        else if (prop.PropertyType == typeof(bool))
                        {
                             // Already handling Enabled manually, but catch others
                             if (prop.Name == "Enabled") continue; 
                             
                             bool val = (bool)prop.GetValue(effect)!;
                             if (ImGui.Checkbox(label, ref val))
                             {
                                 prop.SetValue(effect, val);
                             }
                        }
                        else if (prop.PropertyType == typeof(Vector2))
                        {
                            Vector2 val = (Vector2)prop.GetValue(effect)!;
                            System.Numerics.Vector2 vec2 = new System.Numerics.Vector2(val.X, val.Y);
                            if (ImGui.DragFloat2(label, ref vec2, 0.01f))
                            {
                                prop.SetValue(effect, new Vector2(vec2.X, vec2.Y));
                            }
                        }
                        else if (prop.PropertyType == typeof(Color))
                        {
                            Color val = (Color)prop.GetValue(effect)!;
                            System.Numerics.Vector4 col = val.ToVector4().ToNumerics();
                            if (ImGui.ColorEdit4(label, ref col))
                            {
                                prop.SetValue(effect, new Color(col.X, col.Y, col.Z, col.W));
                            }
                        }
                        else if (prop.PropertyType.IsEnum)
                        {
                            string[] names = Enum.GetNames(prop.PropertyType);
                            int val = (int)prop.GetValue(effect)!;
                            // Find index of current value
                            int index = 0;
                            var values = Enum.GetValues(prop.PropertyType);
                            for (int k = 0; k < values.Length; k++)
                            {
                                if ((int)values.GetValue(k)! == val)
                                {
                                    index = k;
                                    break;
                                }
                            }

                            if (ImGui.Combo(label, ref index, names, names.Length))
                            {
                                prop.SetValue(effect, values.GetValue(index));
                            }
                        }
                    }
                }
            }
        }
        ImGui.End();
    }
}
