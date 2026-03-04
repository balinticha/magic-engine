using DefaultEcs;
using MagicEngine.Engine.Base;
using MagicEngine.Engine.Base.Debug.Commands;
using MagicEngine.Engine.Base.EntitySystem;

namespace MagicEngine.Engine.ECS.Extensions.Terrain.Commands;

public class GenerateChunksCommand : ConsoleCommand
{
    [Dependency] private readonly TerrainSystem _terrainSystem = null!;
    
    public override string Name => "generatechunk";
    public override string Description => "Generate a chunk on a gird. Usage: generatechunk [grid] /grid coords/ [x] [y]";

    public override string Execute(string[] args)
    {
        if (
            args.Length != 3 || 
            !TryGetEntityById(args[0], out Entity grid) ||
            !int.TryParse(args[1], out int x) ||
            !int.TryParse(args[2], out int y)
        )
        {
            return "Invalid args.";
        }

        return $"{_terrainSystem.TryGenerateChunk(grid, new Point2(x, y))}";
    } 
}