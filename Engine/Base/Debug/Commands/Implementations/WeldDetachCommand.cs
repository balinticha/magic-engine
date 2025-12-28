using System;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.ECS.Core.Parenting;

namespace MagicEngine.Engine.Base.Debug.Commands.Implementations;

public class WeldDetachCommand : ConsoleCommand
{
    [Dependency] private readonly HierarchyManager _hierarchyManager = null!;
    
    public override string Name => "weldDetach";
    public override string Description => "Detaches the second entity to the first with a physics weld. Usage: weldDetach <parent> <child>";

    public override string Execute(string[] args)
    {
        if (args.Length < 2 || args.Length > 3)
        {
            return "Invalid arguments. " + Description;
        }
        
        
        if (!TryGetEntityById(args[0], out var parent))
        {
            return $"Error: Could not find an entity with ID '{args[0]}'.";
        }

        if (!TryGetEntityById(args[1], out var child))
        {
            return $"Error: Could not find an entity with ID '{args[1]}'.";
        }
        
        try
        {
            _hierarchyManager.TryDetach(child, parent);
            return $"Command sent.";
        }
        catch (Exception ex)
        {
            return $"Command threw an exception: {ex.Message}";
        }
    }
}