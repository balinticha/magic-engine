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
    [DataField] public int ChunkSize;

    [DataField] public TerrainTileDataDefinition Tileset;
}