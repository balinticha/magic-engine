using DefaultEcs;

namespace MagicEngine.Engine.ECS.Extensions.Terrain;

public class TerrainChunkComponent
{
    // The parent is immortal - it won't die, the ID will remain valid
    public Entity Parent;
}