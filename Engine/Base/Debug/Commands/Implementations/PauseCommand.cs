using System;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.ECS.Core.Session;

namespace MagicEngine.Engine.Base.Debug.Commands.Implementations;

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