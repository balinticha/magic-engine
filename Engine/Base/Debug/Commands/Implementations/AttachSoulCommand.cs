using System;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.ECS.Core.Session;

namespace MagicEngine.Engine.Base.Debug.Commands.Implementations;

public class AttachSoulCommand : ConsoleCommand
{
    [Dependency] private readonly SessionManager _sessionManager = null!;
    
    public override string Name => "attach";
    public override string Description => "Attaches the session soul to a given entity. Usage: attachSoul <target>";

    public override string Execute(string[] args)
    {
        if (args.Length != 1)
        {
            return "Invalid arguments. " + Description;
        }
        
        if (!TryGetEntityById(args[0], out var target))
        {
            return $"Error: Could not find an entity with ID '{args[0]}'.";
        }
        
        try
        {
            _sessionManager.SetControlledEntity(target);
            return $"Command sent.";
        }
        catch (Exception ex)
        {
            return $"Command threw an exception: {ex.Message}";
        }
    }
}