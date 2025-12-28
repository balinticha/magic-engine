using System;

namespace MagicThing.Engine.Base.Debug.Commands.Implementations;

public class AddComponentCommand : ConsoleCommand
{
    public override string Name => "addcomp";
    public override string Description => "Adds a component with the default configuration. Usage: addcomp <target> <component>";

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
            if (HasComponentByType(target, component))
            {
                return $"Entity {target} already has a '{component.Name}' component.";
                
            }
            
            object newComponent = Activator.CreateInstance(component)!;
            if (newComponent == null)
            {
                return $"Error: Could not create an instance of '{component.Name}'. Does it have a default constructor?";
                
            }
            
            SetComponentByType(target, component, newComponent);
            return $"Command sent.";
        }
        catch (Exception ex)
        {
            return $"Command threw an exception: {ex.Message}";
        }
    }
}