using System;

namespace MagicEngine.Engine.Base.Debug.Commands.Implementations;

public class ListScenesCommand : ConsoleCommand
{
    public override string Name => "listscenes";
    public override string Description => "Lists all scenes. Usage: listscenes";

    public override string Execute(string[] args)
    {
        if (args.Length != 0)
        {
            return "Invalid arguments. " + Description;
        }
        
        try
        {
            return $"Command sent. Result: {string.Join(", ", SceneManager.GetSceneNames())}";
        }
        catch (Exception ex)
        {
            return $"Command threw an exception: {ex.Message}";
        }
    }
}