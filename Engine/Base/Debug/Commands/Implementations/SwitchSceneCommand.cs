using System;

namespace MagicEngine.Engine.Base.Debug.Commands.Implementations;

public class SwitchSceneCommand : ConsoleCommand
{
    public override string Name => "setscene";
    public override string Description => "Changes the active scene. Usage: setscene <scene>";

    public override string Execute(string[] args)
    {
        if (args.Length != 1)
        {
            return "Invalid arguments. " + Description;
        }
        
        if (!SceneManager.SceneExists(args[0]))
        {
            return $"Error: Could not find a scene with ID '{args[0]}'.";
        }
        
        try
        {
            return $"Command sent. Result: {SceneManager.TrySetActive(args[0])}";
        }
        catch (Exception ex)
        {
            return $"Command threw an exception: {ex.Message}";
        }
    }
}