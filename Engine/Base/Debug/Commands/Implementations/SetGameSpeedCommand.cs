using System;
using MagicThing.Engine.Base.EntitySystem;
using MagicThing.Engine.ECS.Core.Session;

namespace MagicThing.Engine.Base.Debug.Commands.Implementations;

public class SetGameSpeedCommand : ConsoleCommand
{
    [Dependency] private readonly SessionManager _sessionManager = null!;
    
    public override string Name => "setsimspeed";
    public override string Description => "Sets the simulation speed. Usage: setsimspeed <multiplier> Eg. setsimspeed 1.2";

    public override string Execute(string[] args)
    {
        if (!TryParseNumber<float>(args[0], out float speed))
        {
            return $"Error: Could not parse '{args[0]}' as a number for the Y value.";
        }
        
        try
        {
            _sessionManager.GameSpeed = speed;
            return $"Command sent.";
        }
        catch (Exception ex)
        {
            return $"Command threw an exception: {ex.Message}";
        }
    }
}