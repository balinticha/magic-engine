using System;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.Base.EntityWrappers;

namespace MagicEngine.Engine.Base.Debug.Commands.Implementations;

public class DespawnEntityCommand : ConsoleCommand
{
    [Dependency] private readonly EntityOperationHelpers _entityOperationHelpers = null!;
    public override string Name => "despawn";
    public override string Description => "Despawns a given entity. Usage: despawn <target> <force?>";

    public override string Execute(string[] args)
    {
        if (!(args.Length == 1 || args.Length == 2))
        {
            return "Invalid arguments. " + Description;
        }
        
        if (!TryGetEntityById(args[0], out var target))
        {
            return $"Error: Could not find an entity with ID '{args[0]}'.";
        }
        
        bool force = args.Length == 2 && 
                     (args[1].Equals("true", StringComparison.OrdinalIgnoreCase) ||
                      args[1].Equals("force", StringComparison.OrdinalIgnoreCase) ||
                      args[1].Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                      args[1].Equals("y", StringComparison.OrdinalIgnoreCase) ||
                      args[1].Equals("f", StringComparison.OrdinalIgnoreCase) ||
                      args[1].Equals("-f", StringComparison.OrdinalIgnoreCase) ||
                      args[1] == "1");
        
        try
        {
            if (force)
            {
                _entityOperationHelpers.ForceKillEntity(target);
            }
            else
            {
                _entityOperationHelpers.TryKillEntity(target);
            }
            
            return $"Command sent.";
        }
        catch (Exception ex)
        {
            return $"Command threw an exception: {ex.Message}";
        }
    }
}