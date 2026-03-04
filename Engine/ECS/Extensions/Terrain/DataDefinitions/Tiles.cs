using MagicEngine.Engine.Base.DataDefinitionSystem;
using MagicEngine.Engine.Base.PrototypeComponentSystem;

namespace MagicEngine.Engine.ECS.Extensions.Terrain.DataDefinitions;

[DataDefinition]
public class TerrainTileDataDefinition
{
    [DataField] public IReadOnlyList<TerrainTile> Tiles { get; init; } = new List<TerrainTile>();
}

public struct TerrainTile
{
    [DataField] public string Color;
    [DataField] public string Name;
}