using System;
using System.Collections.Generic;
using System.IO;
using MagicEngine.Engine.Base.EntitySystem;
using Microsoft.Xna.Framework.Input;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MagicEngine.Engine.ECS.Core.Input;


[UpdateInBucket(ExecutionBucket.Input)]
public class InputManagerSystem : EntitySystem
{
    // for ser/deser
    private class BindingConfigData
    {
        public List<BindingItemData> Bindings { get; set; } = new();
    }
    private class BindingItemData
    {
        public string Action { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
    private readonly IDeserializer _deserializer;
    
    private Dictionary<InputAction, InputState> _actions = new Dictionary<InputAction, InputState>();
    private readonly Dictionary<InputAction, InputBinding> _bindings = new Dictionary<InputAction, InputBinding>();
    
    private KeyboardState _currentKeyboardState;
    private KeyboardState _previousKeyboardState;
    private MouseState _currentMouseState;
    private MouseState _previousMouseState;

    public bool IsInputDisabled = false;

    public InputManagerSystem()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    
    public override void Initialize()
    {
        LoadBindingsFromFile();
        base.Initialize();
    }

    public override void Update(Timing timing)
    {
        if (IsInputDisabled) return;
        // Get the latest hardware state
        _currentKeyboardState = Keyboard.GetState();
        _currentMouseState = Mouse.GetState();

        // Iterate through all our configured bindings
        foreach (var kvp in _bindings)
        {
            InputAction action = kvp.Key;
            InputBinding binding = kvp.Value;

            // Check the binding's state in the current and previous frames
            bool isDown = binding.IsDown(_currentKeyboardState, _currentMouseState);
            bool wasDown = binding.IsDown(_previousKeyboardState, _previousMouseState);

            _actions[action] = (isDown, wasDown) switch
            {
                (true, false) => InputState.JustPressed,
                (true, true)  => InputState.Down,
                (false, true) => InputState.JustReleased,
                _                            => InputState.Up
            };

        }
        
        _previousKeyboardState = _currentKeyboardState;
        _previousMouseState = _currentMouseState;
    }
    
    /// <summary>
    /// Public method for other systems to query the state of an action.
    /// </summary>
    public InputState GetActionState(InputAction action)
    {
        return _actions.GetValueOrDefault(action, InputState.Up);
    }
    
    /// <summary>
    /// Public method for other system to query the state of action, when edge cases don't matter (ie: continuous
    /// input)
    /// </summary>
    public bool IsPressed(InputAction action)
    {
        var act = _actions.GetValueOrDefault(action, InputState.Up);
        return act is InputState.Down or InputState.JustPressed;
    }
    
    private void LoadBindingsFromFile()
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs", "bindings.yml");
        
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[ERROR] Input bindings file not found at: {filePath}. Input will not work.");
            return;
        }
        
        try
        {
            var yamlContent = File.ReadAllText(filePath);
            var config = _deserializer.Deserialize<BindingConfigData>(yamlContent);

            foreach (var item in config.Bindings)
            {
                ParseAndAddBinding(item);
            }
            
            Console.WriteLine($"[InputManagerSystem] Successfully loaded {config.Bindings.Count} input bindings.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to parse bindings.yml: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Converts a deserialized binding item into a concrete InputBinding object
    /// and adds it to our main dictionary.
    /// </summary>
    private void ParseAndAddBinding(BindingItemData item)
    {
        // 1. Parse the abstract action (e.g., "MoveUp" -> InputAction.MoveUp)
        if (!Enum.TryParse<InputAction>(item.Action, true, out var action))
        {
            Console.WriteLine($"[WARNING] Invalid action '{item.Action}' in bindings.yml. Skipping.");
            return;
        }

        InputBinding? binding = null;
        
        // 2. Parse the binding type and value
        switch (item.Type.ToLowerInvariant())
        {
            case "key":
                if (Enum.TryParse<Keys>(item.Value, true, out var key))
                {
                    binding = new KeyBinding(key);
                }
                else
                {
                    Console.WriteLine($"[WARNING] Invalid key value '{item.Value}' for action '{item.Action}'. Skipping.");
                }
                break;
                
            case "mouse":
                if (Enum.TryParse<MouseButton>(item.Value, true, out var button))
                {
                    binding = new MouseButtonBinding(button);
                }
                else
                {
                     Console.WriteLine($"[WARNING] Invalid mouse button value '{item.Value}' for action '{item.Action}'. Skipping.");
                }
                break;
                
            default:
                Console.WriteLine($"[WARNING] Unknown binding type '{item.Type}' for action '{item.Action}'. Skipping.");
                break;
        }
        
        // 3. If parsing was successful, add the binding to our dictionary.
        if (binding != null)
        {
            _bindings[action] = binding;
        }
    }
}


public enum InputAction
{
    PrimaryInteract,  // eg. Attack - Left click
    SecondaryInteract,  // eg. Shield - Right click
    MoveUp,  // W
    MoveRight,  // D
    MoveDown,  // S
    MoveLeft,  // A
    Pause,
}

public enum InputState
{
    JustPressed,
    Down,
    JustReleased,
    Up,
}