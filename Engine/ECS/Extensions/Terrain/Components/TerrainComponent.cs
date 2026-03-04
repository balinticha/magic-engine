using DefaultEcs;
using MagicEngine.Engine.Base;
using MagicEngine.Engine.Base.PrototypeComponentSystem;

namespace MagicEngine.Engine.ECS.Extensions.Terrain.Components;

/// <summary>
/// Component for TerrainSystem
/// </summary>
[Component]
public struct TerrainComponent
{
    public Dictionary<Point2, Entity> Chunks;
    [DataField] public int ChunkSize;
    
    
}