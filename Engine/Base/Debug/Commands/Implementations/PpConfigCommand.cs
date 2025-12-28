namespace MagicThing.Engine.Base.Debug.Commands.Implementations;

public class PpConfigCommand : ConsoleCommand
{
    public override string Name => "ppconfig";
    public override string Description => "Toggles the Post Processing Debug Configuration overlay.";

    public override string Execute(string[] args)
    {
        if (PostProcessingManager == null)
        {
            return "Error: PostProcessingManager is not available.";
        }

        PostProcessingManager.IsDebugMenuOpen = !PostProcessingManager.IsDebugMenuOpen;
        return PostProcessingManager.IsDebugMenuOpen 
            ? "Post Process Config overlay ENABLED." 
            : "Post Process Config overlay DISABLED.";
    }
}
