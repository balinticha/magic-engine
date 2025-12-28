using System;

namespace MagicEngine.Engine.Base.PrototypeComponentSystem;

/// <summary>
/// An attribute to mark a class or struct as a component
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ComponentAttribute : Attribute
{
}


[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class LockedComponentAttribute : Attribute
{
}