using System;

namespace MagicEngine.Engine.Base.Debug.Commands.Implementations;

public class ExitCommand : ConsoleCommand
{
    
    public override string Name => "exit";
    public override string Description => "Exits the game. Usage: exit";

    public override string Execute(string[] args)
    {
        try
        {
            Environment.Exit(0);
            return $"Command sent.";
        }
        catch (Exception ex)
        {
            return $"Command threw an exception: {ex.Message}";
        }
    }
}