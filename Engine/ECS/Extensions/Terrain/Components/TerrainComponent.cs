using DefaultEcs;
using MagicEngine.Engine.Base;
using MagicEngine.Engine.Base.PrototypeComponentSystem;
using MagicEngine.Engine.ECS.Extensions.Terrain.DataDefinitions;

namespace MagicEngine.Engine.ECS.Extensions.Terrain.Components;

/// <summary>
/// Component for TerrainSystem
/// </summary>
[Component]
public struct TerrainComponent()
{
    public Dictionary<Point2, Entity> Chunks = new Dictionary<Point2, Entity>();
    [DataField] public int ChunkSize;      // How many tiles per chunk
    [DataField] public int TileSize = 10;  // How big is a tile in game logical coords 

    [DataField] public TerrainTileDataDefinition Tileset;
}