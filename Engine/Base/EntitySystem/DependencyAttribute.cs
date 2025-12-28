using System;

namespace MagicEngine.Engine.Base.EntitySystem;

// This attribute can only be used on fields.
[AttributeUsage(AttributeTargets.Field)]
public sealed class DependencyAttribute : Attribute
{
}