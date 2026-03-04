using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MagicEngine.Engine.Base.PrototypeComponentSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MagicEngine.Engine.Base.DataDefinitionSystem;

/// <summary>
/// Manages loading standalone data definition assets from YAML.
/// </summary>
public class DataDefinitionManager
{
    private readonly ContentManager _content;
    
    // Type -> (Id -> Instance)
    private readonly Dictionary<Type, Dictionary<string, object>> _definitions = new();
    
    // Alias/TypeName -> Type
    private readonly Dictionary<string, Type> _definitionTypes = new();
    
    private readonly IDeserializer _deserializer;

    public bool Initialized { get; private set; }

    /// <summary>
    /// Temporary wrapper to capture the ID and Type before blindly deserializing the rest of the object.
    /// </summary>
    private class YamlDefinitionEnvelope
    {
        [YamlMember(Alias = "id")] 
        public string Id { get; set; } = null!;

        [YamlMember(Alias = "type")] 
        public string Type { get; set; } = null!;

        // The rest of the properties are collected into a dictionary so we can deserialize them into the concrete type.
        [YamlIgnore]
        public Dictionary<string, object> RawData { get; set; } = new();
    }

    public DataDefinitionManager(ContentManager content)
    {
        _content = content;
        
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    public void Initialize()
    {
        if (Initialized) return;

        Console.WriteLine("[DataDefinitionManager] Initializing...");

        DiscoverDefinitionTypes();
        LoadDefinitionsFromDisk();

        Initialized = true;
    }

    private void DiscoverDefinitionTypes()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsDefined(typeof(DataDefinitionAttribute), false));

        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<DataDefinitionAttribute>();
            var typeName = attr?.Alias ?? type.Name;
            
            _definitionTypes[typeName] = type;
            _definitions[type] = new Dictionary<string, object>();
        }

        Console.WriteLine($"[DataDefinitionManager] Discovered {_definitionTypes.Count} definition types.");
    }

    private void LoadDefinitionsFromDisk()
    {
        var root = AppDomain.CurrentDomain.BaseDirectory;
        // Allows both 'Data' or 'Definitions'
        var searchPaths = new[]
        {
            Path.Combine(root, "Data"),
            Path.Combine(root, "Definitions")
        };

        foreach (var path in searchPaths)
        {
            if (!Directory.Exists(path)) continue;

            var files = Directory.GetFiles(path, "*.yaml", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(path, "*.yml", SearchOption.AllDirectories));

            foreach (var file in files)
            {
                try
                {
                    LoadDefinitionsFromFile(file);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[ERROR] DataDefinitionManager failed to load {Path.GetFileName(file)}: {e.Message}");
                }
            }
        }
    }

    private void LoadDefinitionsFromFile(string filePath)
    {
        var yamlContent = File.ReadAllText(filePath);
        
        // Parse into a raw object structure first
        var parsedList = _deserializer.Deserialize<List<Dictionary<string, object>>>(yamlContent);
        if (parsedList == null) return;

        foreach (var rawDict in parsedList)
        {
            if (!rawDict.TryGetValue("id", out var idObj) || !rawDict.TryGetValue("type", out var typeObj))
            {
                Console.WriteLine($"[WARNING] Data definition in {Path.GetFileName(filePath)} missing 'id' or 'type'. Skipping.");
                continue;
            }

            var id = idObj.ToString();
            var typeName = typeObj.ToString();

            if (id == null || typeName == null) continue;

            if (!_definitionTypes.TryGetValue(typeName, out var targetType))
            {
                Console.WriteLine($"[WARNING] Unknown definition type '{typeName}'. Skipping.");
                continue;
            }

            // Remove 'id' and 'type' so we only deserialize the target fields
            rawDict.Remove("id");
            rawDict.Remove("type");

            // Turn it back to YAML just so YamlDotNet can deserialize it clean
            // Not the most performant, but safe and reuses all custom converters
            var serializer = new SerializerBuilder().Build();
            var rawYaml = serializer.Serialize(rawDict);
            
            var instance = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build()
                .Deserialize(rawYaml, targetType);

            if (instance != null)
            {
                _definitions[targetType][id] = instance;
            }
        }
    }

    /// <summary>
    /// Gets a definition instance by its ID.
    /// </summary>
    public T Get<T>(string id) where T : class
    {
        if (_definitions.TryGetValue(typeof(T), out var typeDict) && typeDict.TryGetValue(id, out var data))
        {
            return (T)data;
        }

        throw new KeyNotFoundException($"[DataDefinitionManager] Definition '{id}' of type {typeof(T).Name} not found.");
    }
    
    /// <summary>
    /// Gets a definition instance dynamically by Type and ID.
    /// </summary>
    public object Get(Type type, string id)
    {
        if (_definitions.TryGetValue(type, out var typeDict) && typeDict.TryGetValue(id, out var data))
        {
            return data;
        }

        throw new KeyNotFoundException($"[DataDefinitionManager] Definition '{id}' of type {type.Name} not found.");
    }
    
    public IEnumerable<Type> GetAllDefinitionTypes()
    {
        return _definitionTypes.Values;
    }
}
