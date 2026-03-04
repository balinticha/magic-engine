using DefaultEcs;
using MagicEngine.Engine.Base;

namespace MagicEngine.Engine.ECS.Extensions.Terrain.Components;

public class TerrainChunkComponent
{
    // The parent is immortal - it won't die, the ID will remain valid
    public Entity Parent;
    public Point2 GridPosition;
}