using System;
using MagicThing.Engine.Base.EntitySystem;
using MagicThing.Engine.ECS.Core.Session;

namespace MagicThing.Engine.Base.Debug.Commands.Implementations;

public class PauseCommand : ConsoleCommand
{
    [Dependency] private readonly SessionManager _sessionManager = null!;
    
    public override string Name => "pause";
    public override string Description => "Toggles pause. Usage: pause";

    public override string Execute(string[] args)
    {
        try
        {
            _sessionManager.IsPaused = !_sessionManager.IsPaused;
            return $"Command sent.";
        }
        catch (Exception ex)
        {
            return $"Command threw an exception: {ex.Message}";
        }
    }
}