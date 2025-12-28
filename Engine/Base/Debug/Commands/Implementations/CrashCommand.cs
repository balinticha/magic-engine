using System;

namespace MagicThing.Engine.Base.Debug.Commands.Implementations;

public class CrashCommand : ConsoleCommand
{
    public override string Name => "crash";
    public override string Description => "Crashes the game. Usage: crash <message>";
    
    public override bool AllowCrashes => true;

    public override string Execute(string[] args)
    {
        if (args.Length != 1)
        {
            return "Invalid arguments. " + Description;
        }

        string message = args[0];

        throw new Exception(message);
    }
}