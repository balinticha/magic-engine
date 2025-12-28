using System;

namespace MagicEngine.Engine.Base.Debug.Commands.Implementations;

public class RemoveComponentCommand : ConsoleCommand
{
    public override string Name => "remcomp";
    public override string Description => "Removes a component from an entity. Usage: remcomp <target> <component>";

    public override string Execute(string[] args)
    {
        if (args.Length != 2)
        {
            return "Invalid arguments. " + Description;
        }
        
        if (!TryGetEntityById(args[0], out var target))
        {
            return $"Error: Could not find an entity with ID '{args[0]}'.";
        }
        
        if (!TryFindComponentType(args[1], out var component) || component == null)
            return $"Error: Could not find a component called '{args[1]}'.";
        
        try
        {
            // check first
            if (!HasComponentByType(target, component))
            {
                return $"Entity {target} does not have a '{component.Name}' component.";
                
            }

            // remove second
            RemoveComponentByType(target, component);
            return $"Command sent.";
        }
        catch (Exception ex)
        {
            return $"Command threw an exception: {ex.Message}";
        }
    }
}