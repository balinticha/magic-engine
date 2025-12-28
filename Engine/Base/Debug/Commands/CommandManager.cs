using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DefaultEcs;
using MagicThing.Engine.Base.EntitySystem;
using MagicThing.Engine.Base.Events;
using MagicThing.Engine.Base.PrototypeComponentSystem;
using MagicThing.Engine.Base.Scene;
using MagicThing.Engine.Base.Shaders.PostProcessing;
using MagicThing.Engine.ECS.Core.Camera;

namespace MagicThing.Engine.Base.Debug.Commands;

public class CommandManager
{
    private readonly SystemManager _systemManager;
    private readonly SceneManager _sceneManager;
    private readonly PrototypeManager _prototypeManager;
    private readonly Random _random;
    private readonly CameraSystem _cameraSystem;
    private readonly LogManager _logManager;
    private readonly PostProcessingManager _postProcessingManager;

    private readonly Dictionary<string, IConsoleCommand> _commands = new();

    // Pass in all the dependencies from Game1
    public CommandManager(SystemManager sm, SceneManager s, PrototypeManager prm, Random r, CameraSystem cs, LogManager lm, PostProcessingManager ppm)
    {
        _systemManager = sm;
        _sceneManager = s;
        _prototypeManager = prm;
        _random = r;
        _cameraSystem = cs;
        _logManager = lm;
        _postProcessingManager = ppm;
    }

    public void Initialize()
    {
        var commandTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => 
                typeof(IConsoleCommand).IsAssignableFrom(t) &&
                !t.IsInterface && 
                !t.IsAbstract && 
                !t.IsDefined(typeof(BuiltInCommandAttribute)));

        foreach (var type in commandTypes)
        {
            var commandInstance = (IConsoleCommand)Activator.CreateInstance(type)!;

            // If it inherits from our base class, inject the core systems
            if (commandInstance is ConsoleCommand baseCommand)
            {
                baseCommand.SceneManager = _sceneManager;
                baseCommand.Random = _random;
                baseCommand.Prototypes = _prototypeManager;
                baseCommand.Camera = _cameraSystem;
                baseCommand.SystemManager = _systemManager;
                baseCommand.LogManager = _logManager;
                baseCommand.PostProcessingManager = _postProcessingManager;
            }
            
            // Handle [Dependency] injection for other systems, just like in SystemManager
            var dependencyFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.IsDefined(typeof(DependencyAttribute), false));

            foreach (var field in dependencyFields)
            {
                // systemmanager daddy please help
                var getSystemMethod = typeof(SystemManager).GetMethod(nameof(SystemManager.GetSystem))!
                    .MakeGenericMethod(field.FieldType);
                var dependencyInstance = getSystemMethod.Invoke(_systemManager, null);

                if (dependencyInstance != null)
                {
                    field.SetValue(commandInstance, dependencyInstance);
                }
                else
                {
                     Console.WriteLine($"[CommandManager] WARN: Failed to resolve dependency {field.FieldType.Name} for command {type.Name}");
                }
            }
            
            _commands.Add(commandInstance.Name.ToLower(), commandInstance);
             Console.WriteLine($"[CommandManager] Registered command: '{commandInstance.Name}'");
        }
        
        var helpCommand = new HelpCommand(_commands);
        _commands.Add(helpCommand.Name.ToLower(), helpCommand);
        Console.WriteLine($"[CommandManager] Registered built-in command: '{helpCommand.Name}'");
    }

    public string ExecuteCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var commandName = parts[0].ToLower();
        var args = parts.Skip(1).ToArray();

        if (_commands.TryGetValue(commandName, out var command))
        {
            if (command.AllowCrashes)
            {
                return command.Execute(args);
            }
            else
            {
                try
                {
                    return command.Execute(args);
                }
                catch (Exception ex)
                {
                    return $"ERROR: {ex.Message}";
                }
            }
        }

        return $"Command not found: '{commandName}'";
    }
}