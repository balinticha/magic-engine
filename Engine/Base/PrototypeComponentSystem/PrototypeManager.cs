using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DefaultEcs;
using MagicEngine.Engine.Base.Scene;
using MagicEngine.Engine.ECS.Core.Positioning.Components;
using MagicEngine.Engine.Base.Debug;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MagicEngine.Engine.Base.PrototypeComponentSystem;

/// <summary>
/// Manages loading entity prototypes from YAML files and spawning entities from them.
/// </summary>
public class PrototypeManager
{
    // Dependencies
    private readonly SceneManager _sceneManager;
    private readonly ContentManager _content;

    // Caches
    private readonly Dictionary<string, PrototypeData> _prototypes = new();
    private readonly Dictionary<string, Type> _componentTypeCache = new();
    private readonly Dictionary<Type, Func<object, object>> _typeConverters = new();

    // YAML Deserializer
    private readonly IDeserializer _deserializer;
    
    // Sanity
    public bool Initialized { get; private set; }

    /// <summary>
    /// A temporary data structure that mirrors the YAML structure for easy deserialization.
    /// </summary>
    private class PrototypeData
    {
        [YamlMember(Alias = "id")]
        public string ID { get; set; } = null!;
        [YamlMember(Alias = "name", ApplyNamingConventions = false)]
        public string? Name { get; set; }
        
        [YamlMember(Alias = "parent", ApplyNamingConventions = false)]
        public string? Parent { get; set; }
        public List<Dictionary<string, object>> Components { get; set; } = new();
    }

    public PrototypeManager(SceneManager sceneManager, ContentManager content)
    {
        _sceneManager = sceneManager;
        _content = content;

        // Configure the YAML deserializer to understand standard C# naming conventions.
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    /// <summary>
    /// Initializes the manager by scanning for components and loading all prototypes.
    /// </summary>
    public void Initialize()
    {
        if (Initialized)
            return;
        
        Console.WriteLine("[PrototypeManager] Initializing...");

        // 1. Discover all component types in the current assembly.
        BuildComponentCache();

        // 2. Register default and custom type converters for deserialization.
        RegisterDefaultConverters();

        // 3. Scan the 'Prefabs' directory and load all .yaml files.
        LoadPrototypesFromDisk();

        Console.WriteLine($"[PrototypeManager] Initialization complete. Loaded {_prototypes.Count} prototypes.");
        
        Initialized = true;
    }

    /// <summary>
    /// Registers a custom function to handle the conversion of a YAML value to a specific C# Type.
    /// </summary>
    /// <param name="type">The C# target type (e.g., typeof(MyCustomStruct)).</param>
    /// <param name="converter">A function that takes the raw object from YamlDotNet and returns an instance of the target type.</param>
    public void RegisterTypeConverter(Type type, Func<object, object> converter)
    {
        _typeConverters[type] = converter;
        Console.WriteLine($"[PrototypeManager] Registered custom type converter for {type.Name}");
    }

    /// <summary>
    /// Creates a new entity in the world based on a loaded prototype.
    /// </summary>
    public Entity SpawnEntity(string prototypeId, Vector2 position, Vector2? velocity = null)
    {
        if (!_prototypes.TryGetValue(prototypeId, out var prototype))
        {
            throw new KeyNotFoundException($"Prototype with ID '{prototypeId}' not found.");
        }

        var entity = _sceneManager.GetScene().EcsWorld.CreateEntity();
        
        entity.Set(new PrototypeIDComponent { Value = prototypeId }); 

        entity.Set(new Position { Value = position });
        var vel = velocity ?? Vector2.Zero;
        entity.Set(new Velocity { Value = vel });
        
        if (!string.IsNullOrEmpty(prototype.Name))
        {
            // This assumes a component like: public struct NameComponent { public string Value; }
            entity.Set(new NameComponent { Value = prototype.Name });
        }

        ApplyPrototypeRecursively(entity, prototypeId, new HashSet<string>());
        
        Console.WriteLine($"[PrototypeManager] Spawned entity from prototype '{prototypeId}'.");
        return entity;
    }
    
    private void ApplyPrototypeRecursively(Entity entity, string prototypeId, HashSet<string> processedIds)
    {
        if (!processedIds.Add(prototypeId))
        {
            Console.WriteLine($"[ERROR] Circular dependency detected involving prototype '{prototypeId}'. Aborting spawn chain.");
            return;
        }
        
        if (!_prototypes.TryGetValue(prototypeId, out var prototype))
        {
            Console.WriteLine($"[ERROR] Prototype with ID '{prototypeId}' not found. Aborting spawn chain.");
            return;
        }
        
        // Process parent first to establish base components
        if (!string.IsNullOrEmpty(prototype.Parent))
        {
            ApplyPrototypeRecursively(entity, prototype.Parent, processedIds);
        }
        
        // Now apply/override with this prototype's components
        foreach (var componentData in prototype.Components)
        {
            var typeEntry = componentData.First();
            var componentTypeName = typeEntry.Key;

            // Position and Velocity are set by the SpawnEntity method, so we skip them here
            // to avoid overriding the spawn location.
            if (componentTypeName == nameof(Position) || componentTypeName == nameof(Velocity) || componentTypeName == nameof(NameComponent))
            {
                continue;
            }

            var componentValues = (Dictionary<object, object>)typeEntry.Value;

            if (!_componentTypeCache.TryGetValue(componentTypeName, out var componentType))
            {
                Console.WriteLine($"[ERROR] Unknown component type '{componentTypeName}' in prototype '{prototypeId}'. Skipping.");
                continue;
            }

            // Reflection to get the generic Has<T> and Get<T> methods from DefaultEcs.Entity
            var hasMethod = typeof(Entity).GetMethod(nameof(Entity.Has)).MakeGenericMethod(componentType);
            var getMethod = typeof(Entity).GetMethod(nameof(Entity.Get)).MakeGenericMethod(componentType);
            
            bool componentExists = (bool)hasMethod.Invoke(entity, null);
            object componentInstance;

            if (componentExists)
            {
                // 1. If component exists (from a parent), get the existing instance to modify it.
                componentInstance = getMethod.Invoke(entity, null);
            }
            else
            {
                // 2. If it doesn't exist, create a new one.
                componentInstance = Activator.CreateInstance(componentType);
            }

            // 3. Populate the fields of the new OR existing component instance.
            SetComponentData(ref componentInstance, componentType, componentValues);
            
            // 4. Set the component back on the entity.
            // This is important for both cases:
            // - For new components, it adds them.
            // - For existing STRUCT components, it replaces the old copy with the modified copy.
            // - For existing CLASS components, it's redundant but harmless.
            var setMethod = typeof(Entity).GetMethods().Single(m =>
            {
                if (m.Name != nameof(Entity.Set) || !m.IsGenericMethodDefinition) return false;
                var parameters = m.GetParameters();
                if (parameters.Length != 1) return false;
                bool isByRef = parameters[0].ParameterType.IsByRef;
                return componentType.IsValueType ? isByRef : !isByRef;
            });
            
            setMethod.MakeGenericMethod(componentType).Invoke(entity, new[] { componentInstance });
        }
    }

    private void BuildComponentCache()
    {
        var componentTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => ( t.IsDefined(typeof(ComponentAttribute), false)) || t.IsDefined(typeof(LockedComponentAttribute), false));

        foreach (var type in componentTypes)
        {
            _componentTypeCache[type.Name] = type;
        }
        Console.WriteLine($"[PrototypeManager] Discovered {_componentTypeCache.Count} component types.");
    }

    private void RegisterDefaultConverters()
    {
        // --- Texture2D Converter ---
        // Takes a string asset name from YAML and uses the ContentManager to load it.
        RegisterTypeConverter(typeof(Texture2D), yamlValue =>
        {
            var assetName = yamlValue.ToString();
            if (string.IsNullOrEmpty(assetName)) return null!;
            return _content.Load<Texture2D>(assetName);
        });

        // --- Color Converter ---
        // Takes a string color name from YAML and finds the corresponding static property on the Color struct.
        RegisterTypeConverter(typeof(Color), yamlValue =>
        {
            var colorName = yamlValue.ToString();
            if (string.IsNullOrEmpty(colorName)) return Color.Magenta; // Default for missing color
            var colorProperty = typeof(Color).GetProperty(colorName, BindingFlags.Public | BindingFlags.Static);
            return colorProperty?.GetValue(null) ?? Color.Magenta; // Default for invalid color name
        });

        RegisterTypeConverter(typeof(Vector2), yamlValue =>
        {
            // The YAML parser turns {x: 100, y: 100} into a dictionary.
            var dict = (Dictionary<object, object>)yamlValue;
        
            // We use float.Parse and handle both lowercase and uppercase keys for robustness.
            var x = float.Parse((dict.GetValueOrDefault("x") ?? dict.GetValueOrDefault("X") ?? "0").ToString());
            var y = float.Parse((dict.GetValueOrDefault("y") ?? dict.GetValueOrDefault("Y") ?? "0").ToString());

            return new Vector2(x, y);
        });

        // --- Effect Converter ---
        RegisterTypeConverter(typeof(Effect), yamlValue =>
        {
            var assetName = yamlValue.ToString();
            if (string.IsNullOrEmpty(assetName)) return null!;
            try
            {
                return _content.Load<Effect>(assetName);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[PrototypeManager] Failed to load Effect: {assetName}. {e.Message}");
                return null!;
            }
        });

        // --- Material Parameters Sanitizer ---
        RegisterTypeConverter(typeof(Dictionary<string, object>), yamlValue =>
        {
            var rawDict = (Dictionary<object, object>)yamlValue;
            var sanitized = new Dictionary<string, object>();

            foreach (var kvp in rawDict)
            {
                string key = kvp.Key.ToString()!;
                object value = kvp.Value;

                try 
                {
                    object sanitizedValue = SanitizeParameterValue(value);
                    sanitized[key] = sanitizedValue;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to sanitize parameter '{key}': {e.Message}");
                    throw new Exception($"Failed to sanitize parameter '{key}': {e.Message} in PrototypeManager");
                }
            }
            return sanitized;
        });
    }

    private object SanitizeParameterValue(object rawValue)
    {
        // Handle Lists (Vector2, Vector3, Color from array?)
        if (rawValue is List<object> list)
        {
            if (list.Count == 2)
            {
                return new Vector2(
                    float.Parse(list[0].ToString()!), 
                    float.Parse(list[1].ToString()!));
            }
            if (list.Count == 3)
            {
                return new Vector3(
                    float.Parse(list[0].ToString()!), 
                    float.Parse(list[1].ToString()!),
                    float.Parse(list[2].ToString()!));
            }
            if (list.Count == 4)
            {
                return new Vector4(
                    float.Parse(list[0].ToString()!), 
                    float.Parse(list[1].ToString()!),
                    float.Parse(list[2].ToString()!),
                    float.Parse(list[3].ToString()!));
            }
            
            throw new Exception($"Unsupported list validation size: {list.Count}");
        }
        
        string valStr = rawValue.ToString()!;
        
        if (bool.TryParse(valStr, out bool bVal)) return bVal;

        // Numbers -> Float (Force float for shader compatibility)
        // Check if it looks like a number
        if (double.TryParse(valStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double dVal))
        {
            return (float)dVal;
        }

        // Colors (by Name)
        var colorProp = typeof(Color).GetProperty(valStr, BindingFlags.Public | BindingFlags.Static);
        if (colorProp != null)
        {
            return (Color)colorProp.GetValue(null)!;
        }

        // Check for Comma-Separated Vector Strings (e.g. "1.0, 0.5, 0.0")
        if (valStr.Contains(','))
        {
            var parts = valStr.Split(',')
                .Select(p => p.Trim())
                .ToList();

            if (parts.All(p => float.TryParse(p, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _)))
            {
                var floats = parts.Select(p => float.Parse(p, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                
                if (floats.Length == 2) return new Vector2(floats[0], floats[1]);
                if (floats.Length == 3) return new Vector3(floats[0], floats[1], floats[2]);
                if (floats.Length == 4) return new Vector4(floats[0], floats[1], floats[2], floats[3]);
            }
        }

        // Fallback: String
        return valStr;
    }

    private void LoadPrototypesFromDisk()
    {
        Console.WriteLine("[PrototypeManager] Begin YAML load");
        var root = AppDomain.CurrentDomain.BaseDirectory;
        var prefabsPath = Path.Combine(root, "Prefabs");

        if (!Directory.Exists(prefabsPath))
        {
            Console.WriteLine($"[WARNING] Prefabs directory not found at: {prefabsPath}");
            return;
        }

        var files = Directory.GetFiles(prefabsPath, "*.yaml", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(prefabsPath, "*.yml", SearchOption.AllDirectories));

        foreach (var file in files)
        {
            try
            {
                var yamlContent = File.ReadAllText(file);
                // A single file can contain multiple prototypes (if it's a YAML list).
                var loadedPrototypes = _deserializer.Deserialize<List<PrototypeData>>(yamlContent);

                if (loadedPrototypes == null) continue;

                foreach (var proto in loadedPrototypes)
                {
                    if (string.IsNullOrEmpty(proto.ID))
                    {
                        Console.WriteLine($"[WARNING] Found prototype without an ID in {Path.GetFileName(file)}. Skipping.");
                        continue;
                    }
                    _prototypes[proto.ID] = proto;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to parse prototype file {Path.GetFileName(file)}: {ex.Message}");
            }
        }
    }

    private void SetComponentData(ref object componentInstance, Type componentType, Dictionary<object, object> componentValues)
    {
        // Get all fields and properties of the component marked with [DataField].
        var members = componentType.GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.IsDefined(typeof(DataFieldAttribute), false))
            .Select(m => new
            {
                MemberInfo = m,
                Attribute = m.GetCustomAttribute<DataFieldAttribute>()!
            });

        foreach (var memberData in members)
        {
            var memberInfo = memberData.MemberInfo;
            var attribute = memberData.Attribute;

            // Determine the key to look for in the YAML data.
            // Use the attribute's key if provided, otherwise use the member's name (e.g., "Color" -> "color").
            var yamlKey = attribute.Key ?? CamelCaseNamingConvention.Instance.Apply(memberInfo.Name);

            if (componentValues.TryGetValue(yamlKey, out object yamlValue))
            {
                var targetType = memberInfo switch
                {
                    FieldInfo fi => fi.FieldType,
                    PropertyInfo pi => pi.PropertyType,
                    _ => null
                };

                if (targetType == null) continue;

                try
                {
                    // Convert the raw YAML value to the correct C# type.
                    object convertedValue = ConvertValue(yamlValue, targetType);

                    // Set the value on the component instance.
                    if (memberInfo is FieldInfo fi) fi.SetValue(componentInstance, convertedValue);
                    if (memberInfo is PropertyInfo pi) pi.SetValue(componentInstance, convertedValue);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to set data for field '{memberInfo.Name}' on component '{componentType.Name}'. Reason: {ex.Message}");
                }
            }
        }
    }

    private object ConvertValue(object yamlValue, Type targetType)
    {
        // 1. Check for a registered custom converter first.
        if (_typeConverters.TryGetValue(targetType, out var converter))
        {
            return converter(yamlValue);
        }

        // 2. If no custom converter, try a direct conversion.
        // This handles primitives like int, float, string, bool.
        // For complex types, YamlDotNet might have already deserialized it into a dictionary.
        // We can use its built-in capabilities to perform the final conversion.
        var serializer = new SerializerBuilder().Build();
        var yamlString = serializer.Serialize(yamlValue);
        return new DeserializerBuilder().Build().Deserialize(yamlString, targetType)!;
    }
}

[Component] 
public struct NameComponent
{
    public string Value;
}

[Component]
public struct PrototypeIDComponent
{
    public string Value;
}