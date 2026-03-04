using System;

namespace MagicEngine.Engine.Base.DataDefinitionSystem;

/// <summary>
/// Marks a class or struct as a data definition that can be loaded from YAML files.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DataDefinitionAttribute : Attribute
{
    /// <summary>
    /// The explicit type alias used in the YAML file. If null, the class name should be used.
    /// </summary>
    public string? Alias { get; }

    public DataDefinitionAttribute(string? alias = null)
    {
        Alias = alias;
    }
}
