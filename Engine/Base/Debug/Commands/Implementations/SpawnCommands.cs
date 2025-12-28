using System;
using Microsoft.Xna.Framework;

namespace MagicThing.Engine.Base.Debug.Commands.Implementations;

public class SpawnCommand : ConsoleCommand
{
    public override string Name => "spawn";
    public override string Description => "Spawns an entity from a prototype. Usage: spawn <PrototypeID> <X> <Y>";

    public override string Execute(string[] args)
    {
        if (args.Length < 3)
        {
            return "Invalid arguments. " + Description;
        }

        string prototypeId = args[0];
        if (!float.TryParse(args[1], out float x) || !float.TryParse(args[2], out float y))
        {
            return "Invalid coordinates. X and Y must be numbers.";
        }
        
        var position = new Vector2(x, y);
        
        try
        {
            var newEntity = Prototypes.SpawnEntity(prototypeId, position);
            return $"Successfully spawned '{prototypeId}' at {position} with Entity {newEntity}.";
        }
        catch (Exception ex)
        {
            return $"Failed to spawn entity: {ex.Message}";
        }
    }
}