namespace MagicEngine.Engine.Base.Debug.Commands;

public interface IConsoleCommand
{
    /// <summary>
    /// The keyword used to invoke the command (e.g., "spawn", "kill").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// A brief description of what the command does, for a 'help' command.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Should the command be executed in a try / catch block?
    /// </summary>
    bool AllowCrashes { get; }

    /// <summary>
    /// Executes the command's logic.
    /// </summary>
    /// <param name="args">The arguments passed to the command, split by spaces.</param>
    /// <returns>A message to be displayed in the console as a result.</returns>
    string Execute(string[] args);
}