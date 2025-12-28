// MagicThing/Debug/Attributes/InspectorAttributes.cs

using System;

namespace MagicThing.Engine.Base.Debug.Attributes;

/// <summary>
/// Base class for all inspector-related attributes. Good practice but not strictly required.
/// The attribute can only be applied to fields.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public abstract class InspectorAttribute : Attribute { }

/// <summary>
/// When added to a public field, allows its value to be edited in the component viewer.
/// </summary>
public class InspectorEditableAttribute : InspectorAttribute { }

/// <summary>
/// When added to a public numeric field (float or int), displays it as a slider
/// in the component viewer within the specified min/max range.
/// </summary>
public class InspectorSliderAttribute : InspectorAttribute
{
    public float Min { get; }
    public float Max { get; }

    public InspectorSliderAttribute(float min, float max)
    {
        Min = min;
        Max = max;
    }
}
