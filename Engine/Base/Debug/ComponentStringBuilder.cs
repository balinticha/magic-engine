using System.Reflection;
using System.Text;
using DefaultEcs;
using DefaultEcs.Serialization;

namespace MagicThing.Engine.Base.Debug;

// This class implements the interface DefaultECS needs to read components.
public class ComponentStringBuilder : IComponentReader
{
    private readonly StringBuilder _builder;

    public ComponentStringBuilder(StringBuilder builder)
    {
        _builder = builder;
    }

    // This method is called by DefaultECS for each component on the entity.
    public void OnRead<T>(in T component, in Entity componentOwner)
    {
        var type = typeof(T);
        _builder.AppendLine($"- Component: {type.Name}");

        // Use reflection to get all public instance fields
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        if (fields.Length == 0)
        {
            // If no fields, maybe it's a simple value type or has properties
            _builder.AppendLine($"  Value: {component?.ToString() ?? "null"}");
        }
        else
        {
            foreach (var field in fields)
            {
                object value = field.GetValue(component);
                _builder.AppendLine($"  - {field.Name}: {value?.ToString() ?? "null"}");
            }
        }
        _builder.AppendLine(); // Add a blank line for readability
    }
}

public static class EntityDebugExtensions
{
    public static string GetDebugString(this Entity entity)
    {
        if (!entity.IsAlive)
        {
            return "Entity is not alive.";
        }

        var builder = new StringBuilder();
        builder.AppendLine($"--- Entity {entity.GetHashCode()} ---");

        var reader = new ComponentStringBuilder(builder);

        // This is the key DefaultECS method that triggers the process
        entity.ReadAllComponents(reader);

        if (builder.Length < 30) // A bit arbitrary, checks if any components were added
        {
            builder.AppendLine("(No components)");
        }

        return builder.ToString();
    }
}