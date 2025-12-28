using System.Collections.Generic;
using System.Linq;

namespace MagicThing.Engine.Base.Debug.Commands;

[BuiltInCommand]
public class HelpCommand : ConsoleCommand
{
    private readonly Dictionary<string, IConsoleCommand> _allCommands;

    public override string Name => "help";
    public override string Description => "Shows a list of all commands, or detailed help for a specific command.";
    
    public HelpCommand(Dictionary<string, IConsoleCommand> commands)
    {
        _allCommands = commands;
    }

    public override string Execute(string[] args)
    {
        // "help" (no arguments)
        if (args.Length == 0)
        {
            var commandNames = string.Join(", ", _allCommands.Keys.OrderBy(name => name));
            return $"Available commands: {commandNames}";
        }

        // "help <command_name>"
        if (args.Length == 1)
        {
            var commandName = args[0].ToLower();
            if (_allCommands.TryGetValue(commandName, out var command))
            {
                return $"{command.Name}: {command.Description}";
            }
            
            return $"Command not found: '{commandName}'";
        }
        
        // Too many arguments
        return "Usage: help [command_name]";
    }
}