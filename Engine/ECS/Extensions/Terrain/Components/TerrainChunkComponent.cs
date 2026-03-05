using DefaultEcs;
using MagicEngine.Engine.Base;

namespace MagicEngine.Engine.ECS.Extensions.Terrain.Components;

/// <summary>
/// The internal component assigned to a chunk. It is managed by the terrain system, and therefore,
/// not assignably by YAML.
/// </summary>
public struct TerrainChunkComponent(int chunkSize)
{
    // The parent is immortal - it won't die, the ID will remain valid
    public Entity Parent;
    public Point2 GridPosition;
    // the multiplier used to calculate physical size of a single tile
    public int TileSize;
    public readonly int ChunkSize = chunkSize;
    
    // flattened row-major grid. [x,y] = y * size + x 
    public int[] Tiles = new int[chunkSize * chunkSize];
    
    // shorthand indexer for accessing Tiles
    public int this[int x, int y]
    {
        get => Tiles[y * ChunkSize + x];
        set => Tiles[y * ChunkSize + x] = value;
    }

    public int this[int i]
    {
        get => Tiles[i];
        set => Tiles[i] = value;
    }

    // shorthand enumerator for enumerating Tiles
    public Enumerator GetEnumerator() => new Enumerator(Tiles);
    
    public ref struct Enumerator
    {
        private readonly int[] _tiles;
        private int _index;

        public Enumerator(int[] tiles)
        {
            _tiles = tiles;
            _index = -1;
        }

        public bool MoveNext()
        {
            _index++;
            return _index < _tiles.Length;
        }

        public int Current => _tiles[_index];
    }
}