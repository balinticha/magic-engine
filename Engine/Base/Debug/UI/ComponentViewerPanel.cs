using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DefaultEcs;
using DefaultEcs.Serialization;
using ImGuiNET;
using MagicEngine.Engine.Base.Debug.Attributes;
using MagicEngine.Engine.Base.PrototypeComponentSystem;
using MagicEngine.Engine.ECS.Core.Parenting.Components;
using MagicEngine.Engine.ECS.Core.Positioning.Components;
using Microsoft.Xna.Framework;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace MagicEngine.Engine.Base.Debug.UI;

/// <summary>
/// A helper class that implements IComponentReader to extract all component instances from an entity.
/// </summary>
internal class ComponentCollector : IComponentReader
{
    public readonly List<object> Components = new();
    public void OnRead<T>(in T component, in Entity componentOwner)
    {
        Components.Add(component);
    }
}

public class ComponentViewerPanel
{
    // Define the types for each category for easy lookup
    private readonly HashSet<Type> _innateTypes = new()
    {
        typeof(Position), typeof(Velocity), typeof(NameComponent), typeof(PrototypeIDComponent)
    };
    
    private readonly HashSet<Type> _hierarchyTypes = new()
    {
        typeof(IsParent), typeof(IsChildren)
    };

    public void Draw(Entity entity, ref bool isOpen, int uniqueId)
    {
        if (!isOpen)
        {
            return;
        }
        
        // If no entity is selected or the selected entity is dead, don't draw the window.
        if (!entity.IsAlive)
        {
            isOpen = false;
            return;
        }

        ImGui.SetNextWindowSize(new Vector2(350, 500), ImGuiCond.FirstUseEver);
        
        var displayEntityName = $"{entity}".Substring(7) + "E";
        string windowTitle = $"Component Viewer: {displayEntityName}##{uniqueId}";
        
        // Use the entity's hash code for a unique window ID
        if (ImGui.Begin(windowTitle, ref isOpen, ImGuiWindowFlags.MenuBar))
        {
            var collector = new ComponentCollector();
            entity.ReadAllComponents(collector);
            
            var innateComponents = new List<object>();
            var hierarchyComponents = new List<object>();
            var otherComponents = new List<object>();
            var tagComponents = new List<object>();

            foreach (var component in collector.Components)
            {
                var type = component.GetType();
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

                // A component with no public fields is considered a "Tag".
                if (fields.Length == 0)
                {
                    tagComponents.Add(component);
                }
                else if (_innateTypes.Contains(type))
                {
                    innateComponents.Add(component);
                }
                else if (_hierarchyTypes.Contains(type))
                {
                    hierarchyComponents.Add(component);
                }
                else
                {
                    otherComponents.Add(component);
                }
            }
            
            // --- Draw Categories ---
            DrawComponentCategory("Innate", innateComponents, entity);
            DrawComponentCategory("Hierarchy", hierarchyComponents, entity);
            DrawComponentCategory("Other Components", otherComponents, entity);
            DrawTagCategory("Tags", tagComponents);
        }
        ImGui.End();
    }

    private void DrawComponentCategory(string label, List<object> components, Entity entity)
    {
        if (!components.Any()) return;

        // Use a separator for clarity between categories
        ImGui.SeparatorText(label);
        
        bool startUncollapsed = (label == "Innate");

        foreach (var component in components)
        {
            DrawComponentFields(component, entity, startUncollapsed);
        }
    }
    
    private void DrawTagCategory(string label, List<object> components)
    {
        if (!components.Any()) return;

        ImGui.SeparatorText(label);

        foreach (var component in components)
        {
            // Use a simple bullet point for a clean list of tags
            ImGui.BulletText(component.GetType().Name);
        }
    }

    private void DrawComponentFields(object component, Entity entity, bool startUncollapsed)
    {
        var type = component.GetType();
        var flags = startUncollapsed ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
        
        // Each component gets its own collapsible header
        if (ImGui.CollapsingHeader(type.Name, flags))
        {
            ImGui.PushID(type.FullName);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            bool componentModified = false;

            foreach (var field in fields)
            {
                // Draw a widget for the field and track if it was changed
                if (DrawFieldWidget(component, field))
                {
                    componentModified = true;
                }
            }
            
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (prop.PropertyType == typeof(Dictionary<string, object>))
                {
                    if (DrawDictionary(component, prop))
                    {
                        componentModified = true;
                    }
                }
            }
            
            // If any field was changed, update the component on the entity.
            // This is crucial for structs, as we are modifying a boxed copy.
            if (componentModified)
            {
                SetComponentReflectively(entity, component);
            }
            ImGui.PopID();
        }
    }

    /// <summary>
    /// Draws the correct ImGui widget for a given field based on its attributes and type.
    /// Handles both editable and read-only display.
    /// </summary>
    /// <returns>True if the field's value was changed, otherwise false.</returns>
    private bool DrawFieldWidget(object component, FieldInfo field)
    {
        var value = field.GetValue(component);
        var fieldType = field.FieldType;
        bool valueChanged = false;

        // Check for inspector attributes
        var sliderAttr = field.GetCustomAttribute<InspectorSliderAttribute>();
        var editableAttr = field.GetCustomAttribute<InspectorEditableAttribute>();

        // --- Slider Widget ---
        if (sliderAttr != null)
        {
            if (fieldType == typeof(float))
            {
                var val = (float)value;
                if (ImGui.SliderFloat(field.Name, ref val, sliderAttr.Min, sliderAttr.Max))
                {
                    field.SetValue(component, val);
                    valueChanged = true;
                }
            }
            else if (fieldType == typeof(int))
            {
                var val = (int)value;
                if (ImGui.SliderInt(field.Name, ref val, (int)sliderAttr.Min, (int)sliderAttr.Max))
                {
                    field.SetValue(component, val);
                    valueChanged = true;
                }
            }
        }
        // --- General Editable Widget ---
        else if (editableAttr != null)
        {
            // --- Number Types ---
            if (fieldType == typeof(float))
            {
                var val = (float)value;
                if (ImGui.DragFloat(field.Name, ref val, 0.1f))
                {
                    field.SetValue(component, val);
                    valueChanged = true;
                }
            }
            else if (fieldType == typeof(int))
            {
                var val = (int)value;
                if (ImGui.DragInt(field.Name, ref val))
                {
                    field.SetValue(component, val);
                    valueChanged = true;
                }
            }
            // --- Text ---
            else if (fieldType == typeof(string))
            {
                var val = (string)value ?? "";
                if (ImGui.InputText(field.Name, ref val, 256))
                {
                    field.SetValue(component, val);
                    valueChanged = true;
                }
            }
            // --- Boolean ---
            else if (fieldType == typeof(bool))
            {
                var val = (bool)value;
                if (ImGui.Checkbox(field.Name, ref val))
                {
                    field.SetValue(component, val);
                    valueChanged = true;
                }
            }
            // --- Vector2 ---
            else if (fieldType == typeof(Microsoft.Xna.Framework.Vector2))
            {
                var val = (Microsoft.Xna.Framework.Vector2)value;
                var sysVec = new Vector2(val.X, val.Y);
                if (ImGui.DragFloat2(field.Name, ref sysVec, 0.1f))
                {
                    field.SetValue(component, new Microsoft.Xna.Framework.Vector2(sysVec.X, sysVec.Y));
                    valueChanged = true;
                }
            }
            // --- Color ---
            else if (fieldType == typeof(Color))
            {
                var val = (Color)value;
                var sysVec = new Vector4(val.R / 255f, val.G / 255f, val.B / 255f, val.A / 255f);
                if (ImGui.ColorEdit4(field.Name, ref sysVec))
                {
                    var newColor = new Color(sysVec.X, sysVec.Y, sysVec.Z, sysVec.W);
                    field.SetValue(component, newColor);
                    valueChanged = true;
                }
            }
        }
        // --- Read-Only Display for fields without attributes ---
        else
        {
            ImGui.Text($"{field.Name}:");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));

            // Special handling for displaying entity references
            if (value is Entity entityRef)
            {
                ImGui.Text($"{($"{entityRef}").Substring(7)}E");
            }
            else if (value is List<Entity> entityList)
            {
                ImGui.Text($"List<Entity> ({entityList.Count} items)");
            }
            else
            {
                ImGui.Text(value?.ToString() ?? "null");
            }
            ImGui.PopStyleColor();
        }

        return valueChanged;
    }

    private bool DrawDictionary(object component, PropertyInfo property)
    {
        var dict = (Dictionary<string, object>)property.GetValue(component);
        if (dict == null) return false;

        bool changed = false;
        if (ImGui.TreeNode(property.Name))
        {
            var keys = dict.Keys.ToList();
            keys.Sort();

            foreach (var key in keys)
            {
                var val = dict[key];
                ImGui.PushID(key);
                
                ImGui.AlignTextToFramePadding();
                ImGui.Text(key);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(200);

                object editableValue = val;
                bool isParsedFromString = false;

                // Try to parse string values into editable types
                if (val is string str)
                {
                    if (TryParseStringValue(str, out var parsed))
                    {
                        editableValue = parsed;
                        isParsedFromString = true;
                    }
                }

                // Draw widget
                if (DrawValueWidget("##v", ref editableValue))
                {
                    dict[key] = editableValue;
                    changed = true;
                }
                else if (!isParsedFromString && val is string)
                {
                     // Show string text if we couldn't parse it
                     ImGui.TextDisabled($"\"{val}\"");
                }
                else if (val == null)
                {
                     ImGui.TextDisabled("null");
                }
                // If it was valid but we didn't have a widget (e.g. unknown object), DrawValueWidget returns false and no UI?
                // We should handle the fallback in DrawValueWidget or here.
                
                ImGui.PopID();
            }
            ImGui.TreePop();
        }

        if (changed)
        {
            property.SetValue(component, dict);
        }
        return changed;
    }

    private bool TryParseStringValue(string str, out object result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(str)) return false;

        // Try float
        if (float.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float f))
        {
            result = f;
            return true;
        }

        // Try Vectors
        var parts = str.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(s => s.Trim())
                       .ToArray();
        
        if (parts.Length == 2)
        {
            if (float.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float y))
            {
                result = new Microsoft.Xna.Framework.Vector2(x, y);
                return true;
            }
        }
        else if (parts.Length == 3)
        {
            if (float.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float y) &&
                float.TryParse(parts[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float z))
            {
                result = new Microsoft.Xna.Framework.Vector3(x, y, z);
                return true;
            }
        }
        else if (parts.Length == 4)
        {
            if (float.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float y) &&
                float.TryParse(parts[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float z) &&
                float.TryParse(parts[3], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float w))
            {
                // Prefer Vector4 over Color for general numbers, but user can assume Color?
                // Usually shaders take float4 for color anyway.
                result = new Microsoft.Xna.Framework.Vector4(x, y, z, w);
                return true;
            }
        }
        return false;
    }

    private bool DrawValueWidget(string label, ref object value)
    {
        bool changed = false;

        if (value is float f)
        {
            if (ImGui.DragFloat(label, ref f, 0.05f)) { value = f; changed = true; }
            return changed;
        }
        if (value is int i)
        {
            if (ImGui.DragInt(label, ref i)) { value = i; changed = true; }
            return changed;
        }
        if (value is bool b)
        {
            if (ImGui.Checkbox(label, ref b)) { value = b; changed = true; }
            return changed;
        }
        if (value is Microsoft.Xna.Framework.Vector2 xv2)
        {
            var sysVec = new Vector2(xv2.X, xv2.Y);
            if (ImGui.DragFloat2(label, ref sysVec, 0.1f)) 
            { 
                value = new Microsoft.Xna.Framework.Vector2(sysVec.X, sysVec.Y); 
                changed = true; 
            }
            return changed; // If we return here, we assume we drew the widget
        }
        if (value is Microsoft.Xna.Framework.Vector3 xv3)
        {
            var sysVec = new System.Numerics.Vector3(xv3.X, xv3.Y, xv3.Z);
            if (ImGui.DragFloat3(label, ref sysVec, 0.1f)) 
            { 
                value = new Microsoft.Xna.Framework.Vector3(sysVec.X, sysVec.Y, sysVec.Z); 
                changed = true; 
            }
            return changed;
        }
        if (value is Microsoft.Xna.Framework.Vector4 xv4)
        {
            var sysVec = new Vector4(xv4.X, xv4.Y, xv4.Z, xv4.W);
            if (ImGui.DragFloat4(label, ref sysVec, 0.1f)) 
            { 
                value = new Microsoft.Xna.Framework.Vector4(sysVec.X, sysVec.Y, sysVec.Z, sysVec.W); 
                changed = true; 
            }
            return changed;
        }
        if (value is Color c)
        {
            var vec4 = c.ToVector4();
            var sysVec = new Vector4(vec4.X, vec4.Y, vec4.Z, vec4.W);
            if (ImGui.ColorEdit4(label, ref sysVec)) 
            { 
                value = new Color(sysVec.X, sysVec.Y, sysVec.Z, sysVec.W); 
                changed = true; 
            }
            return changed;
        }

        // If we reached here, we didn't draw an editable widget.
        // But for 'DrawDictionary' logic, we want to know if we 'handled' it.
        // Actually, DrawDictionary handles the fallback drawing if this returns false? 
        // No, changed=false just means valid widget but no change.
        // We need to signal "Did we draw a widget?".
        // Whatever, let's just draw the Disabled text HERE if it's an unknown type that isn't null.
        if (value != null)
        {
             ImGui.TextDisabled($"{value.GetType().Name}: {value}");
        }
        return false;
    }
    
    /// <summary>
    /// Uses reflection to call the generic entity.Set T () method.
    /// This is required because older versions of DefaultEcs lack the non-generic SetBoxed() method.
    /// </summary>
    private void SetComponentReflectively(Entity entity, object component)
    {
        var componentType = component.GetType();

        // Find the correct Set<T> method. For structs, it takes a 'ref' parameter; for classes, it does not.
        // This LINQ query accurately finds the correct overload.
        var setMethodInfo = typeof(Entity).GetMethods()
            .Single(m =>
            {
                if (m.Name != nameof(Entity.Set) || !m.IsGenericMethodDefinition) return false;
                var parameters = m.GetParameters();
                if (parameters.Length != 1) return false;
                // Check if the parameter is passed by reference (for structs) or by value (for classes)
                return componentType.IsValueType ? parameters[0].ParameterType.IsByRef : !parameters[0].ParameterType.IsByRef;
            });

        // Create a generic method instance (e.g., Set<Position>) and invoke it
        var genericSetMethod = setMethodInfo.MakeGenericMethod(componentType);
        genericSetMethod.Invoke(entity, new[] { component });
    }
}