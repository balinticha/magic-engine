using System;

namespace MagicEngine.Engine.Base.PrototypeComponentSystem;

/// <summary>
/// An attribute to mark a field loadable from YAML
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class DataFieldAttribute : Attribute
{
    /// <summary>
    /// The key to look for in the YAML data. If null, the C# field/property name is used.
    /// </summary>
    public string? Key { get; }

    /// <summary>
    /// Marks a field or property to be loaded from prototype data.
    /// </summary>
    /// <param name="key">The key to look for in the YAML file. If not provided,
    /// the name of the C# member is used.</param>
    public DataFieldAttribute(string? key = null)
    {
        Key = key;
    }
}