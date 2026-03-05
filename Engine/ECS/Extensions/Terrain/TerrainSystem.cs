using DefaultEcs;
using MagicEngine.Engine.Base;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.Base.EntitySystem.Time;
using MagicEngine.Engine.Base.EntityWrappers;
using MagicEngine.Engine.ECS.Core.Events.EntityDeath;
using MagicEngine.Engine.ECS.Core.Physics.Bridge.Components;
using MagicEngine.Engine.ECS.Core.Positioning.Components;
using MagicEngine.Engine.ECS.Extensions.Terrain.Components;
using Microsoft.Xna.Framework;

namespace MagicEngine.Engine.ECS.Extensions.Terrain;

/// <summary>
/// The engine extension system that acts as a fully featured terrain grid. This manages it's associated terrain chunk.
/// The grid's internal grid coordinate grid starts at 0,0 at the top left, and y increases downwards.
/// The grid's Positon component's coordiante will specify the top left coordinate of the tile at the grid coordinate 0, 0
///
/// An entity with a TerrainComponent cannot be destroyed naturally.
/// </summary>
public class TerrainSystem : EntitySystem
{
    [Dependency] private readonly TerrainChunkSystem _chunkSystem = null!;
    
    public override void OnSceneLoad()
    {
        Events.Subscribe<TerrainComponent, EntityDeathRequestEvent>(OnTerrainDeathRequest);
        Events.Subscribe<TerrainComponent, ForcedEntityDeathRequestEvent>(OnTerrainForcedDeathRequest);
        Events.Subscribe<TerrainChunkComponent, EntityDeathEvent>(OnChunkDeath);
    }

    #region Engine related housekeeping
    private void OnTerrainDeathRequest(Entity<TerrainComponent> ent, EntityDeathRequestEvent ev)
    {
        ev.IsCancelled = true;
    }

    private void OnTerrainForcedDeathRequest(Entity<TerrainComponent> ent, ForcedEntityDeathRequestEvent ev)
    {
        ev.CancelAndRaiseFatalError = true;
    }

    private void OnChunkDeath(Entity<TerrainChunkComponent> ent, EntityDeathEvent ev)
    {
        if (!ent.Comp.Parent.TryGet<TerrainComponent>(out var terrain))
        {
            throw new Exception("Broken link from TerrainChunk -> Parent Terrain entity. Something is seriously wrong!");
        }
        
        // Since we just verified that the terrain HAS the chunk, if it turns out that it actually doesn't have it
        // well... crash and panic... and then cry for 10 hours while debugging
        terrain.Comp.Chunks.Remove(Where(terrain.Owner, ent.Owner)!.Value);
    }
    
    /// <summary>
    /// Converts world global coordinates to grid specific chunk integer coordinates. Ie. which chunk does a given point
    /// belong to?
    /// </summary>
    /// <param name="gridOrigin">Starting global coordinate of the grid</param>
    /// <param name="chunkSize">Chunk size</param>
    /// <param name="queryPoint">Queried point</param>
    /// <returns></returns>
    private Point2? GlobalToGridCoords(Vector2 gridOrigin, int chunkSize, Vector2 queryPoint)
    {
        queryPoint.Deconstruct(out var x, out var y);
        
        // convert world coordinates to grid-relative world coordinates
        float rx, ry;
        rx = x - gridOrigin.X;
        ry = y - gridOrigin.Y;

        // out of bounds
        if (rx < 0 || ry < 0)
        {
            return null;
        }
        
        // convert to grid chunk coords
        int gx = (int)rx / chunkSize;
        int gy = (int)ry / chunkSize;
        
        return new Point2(gx, gy);
    }

    /// <summary>
    /// Get the entityID of the chunk at a given position.
    /// Returns null if no chunk is at a given position.
    /// </summary>
    /// <param name="grid">The entity being queried</param>
    /// <param name="queryGlobalPosition">The WORLD COORDINATES of the queried position</param>
    /// <returns></returns>
    public Entity? GetChunkAt(Entity grid, Vector2 queryGlobalPosition)
    {
        if (!grid.TryGet<TerrainComponent>(out var terrain))
        {
            return null;
        }

        if (!grid.TryGet<Position>(out var gridPos))
        {
            return null;
        }

        Point2? gridPoint = GlobalToGridCoords(gridPos.Comp.Value, terrain.Comp.ChunkSize, queryGlobalPosition);
        
        if (gridPoint == null)
        {
            return null;
        }

        if (!terrain.Comp.Chunks.TryGetValue(gridPoint.Value, out var chunk))
        {
            return null;
        }

        return chunk;
    }

    /// <summary>
    /// Get the entityID of the chunk at a given chunk position.
    /// Returns null if no chunk is at a given position.
    /// </summary>
    /// <param name="grid">The terrain being queried</param>
    /// <param name="queryGridPosition">The GRID CHUNK coordinates of the queried position</param>
    /// <returns></returns>
    public Entity? GetChunkAt(Entity grid, Point2 queryGridPosition)
    {
        if (!grid.TryGet<TerrainComponent>(out var terrain))
        {
            return null;
        }
        
        if (!terrain.Comp.Chunks.TryGetValue(queryGridPosition, out var chunk))
        {
            return null;
        }

        return chunk;
    }

    /// <summary>
    /// Returns the grid local coords of a given chunk, or null if the grid does not have the chunk.
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="chunk"></param>
    /// <returns></returns>
    // TODO optimize this to be O(1)
    public Point2? Where(Entity grid, Entity chunk)
    {
        if (chunk.Has<TerrainComponent>() || grid.Has<TerrainChunkComponent>())
        {
            return null;
        }
        
        if (grid.Equals(chunk)) {
            return null;
        }

        if (!chunk.TryGet<TerrainChunkComponent>(out var chunkC))
        {
            return null;
        }

        // not this grid
        if (chunkC.Comp.Parent != grid)
        {
            return null;
        }

        return chunkC.Comp.GridPosition;
    }

    /// <summary>
    /// Does the given grid has a chunk at a given WORLD position?
    /// </summary>
    public bool HasChunk(Entity grid, Vector2 queryPosition)
    {
        return GetChunkAt(grid, queryPosition) != null;
    }

    /// <summary>
    /// Does the given grid has a chunk at a given GRID position (ie. does a chunk exist in grid local space)
    /// </summary>
    public bool HasChunk(Entity grid, Point2 queryPosition)
    {
        return GetChunkAt(grid, queryPosition) != null;
    }

    /// <summary>
    /// Does a given grid contains a given chunk?
    /// </summary>
    public bool HasChunk(Entity grid, Entity chunk)
    {
        // nonsense requests
        if (chunk.Has<TerrainComponent>() || grid.Has<TerrainChunkComponent>())
        {
            return false;
        }
        
        if (grid.Equals(chunk)) {
            return false;
        }

        if (!grid.TryGet<TerrainComponent>(out var terrainComp))
        {
            return false;
        }

        if (!chunk.TryGet<TerrainChunkComponent>(out var chunkComp))
        {
            return false;
        }
        
        return terrainComp.Owner.Equals(chunkComp.Comp.Parent);
    }
    #endregion

    #region Adding, Removing chunks
    /// <summary>
    /// Add a chunk to a grid safely. Sets parent and grid position on the chunk.
    /// </summary>
    public void AddChunk(Entity grid, Entity chunk, Point2 attachToPosition)
    {
        if (chunk.Has<TerrainComponent>())
        {
            throw new InvalidOperationException("Tried to attach a terrain as a chunk to another terrain");
        }
        
        if (grid.Equals(chunk)) {
            throw new InvalidOperationException("Tried to attach a chunk to itself");
        }

        if (!grid.TryGet<TerrainComponent>(out var terrain))
        {
            throw new InvalidOperationException("Tried to attach a chunk to a non-terrain component");
        }

        if (!chunk.TryGet<TerrainChunkComponent>(out var chunkComp))
        {
            throw new InvalidOperationException("Tried to attach a non-chunk to a terrain");
        }

        if (terrain.Comp.Chunks.ContainsKey(attachToPosition))
        {
            throw new InvalidOperationException("Tried to attach a chunk to a taken position");
        }
        
        terrain.Comp.Chunks.Add(attachToPosition, chunk);
        chunkComp.Comp.GridPosition = attachToPosition;
        chunkComp.Comp.Parent = terrain.Owner;

        ref var gridPos = ref grid.Get<Position>();
        
        // how big a chunk is in game coordinates
        var chunkLogicalSize = terrain.Comp.ChunkSize * terrain.Comp.TileSize;
        Vector2 targetLogicalPosTopLeftRelative = new Vector2(
            attachToPosition.X * chunkLogicalSize, attachToPosition.Y *  chunkLogicalSize);
        
        Vector2 targetLogicalPosTopLeftGlobal = gridPos.Value + targetLogicalPosTopLeftRelative;
        
        ref var chunkPos = ref chunk.Get<Position>();
        chunkPos.Value = targetLogicalPosTopLeftGlobal;
    }

    public bool TryRemoveChunk(Entity grid, Entity chunk)
    {
        if (!HasChunk(grid, chunk))
        {
            return false;
        }

        if (!grid.TryGet<TerrainComponent>(out var terrain))
        {
            return false;
        }
        
        terrain.Comp.Chunks.Remove(Where(grid, chunk)!.Value);
        return true;
    }

    public bool TryRemoveChunk(Entity grid, Point2 chunkPosition)
    {
        if (!HasChunk(grid, chunkPosition))
        {
            return false;
        }
        
        if (!grid.TryGet<TerrainComponent>(out var terrain))
        {
            return false;
        }
        
        terrain.Comp.Chunks.Remove(chunkPosition);
        return true;
    }
    #endregion
    
    /// <summary>
    /// Generates and attaches a chunk to the grid at a given position.
    /// This is meant to be a sample implementation to be completely overridden by user systems implementing
    /// a terrain. This assumes that the terrain's tileset has at least two tiles defined.
    /// </summary>
    /// <param name="grid">The terrain we are generating a chunk on</param>
    /// <param name="chunkPosition">The terrain</param>
    /// <returns></returns>
    public bool TryGenerateChunk(Entity grid, Point2 chunkPosition)
    {
        if (!grid.TryGet<TerrainComponent>(out var terrain))
        {
            return false;
        }
        
        if (HasChunk(grid, chunkPosition))
        {
            return false;
        }
        
        // This demo implementation assumes there are at least two items in the tileset
        Entity chunkEnt = Prototypes.SpawnEntity("TerrainChunk");
        
        TerrainChunkComponent chunkCmp = new TerrainChunkComponent(terrain.Comp.ChunkSize);
        chunkCmp.TileSize = terrain.Comp.TileSize;
        chunkCmp[0, 0] = 0;
        for (int i = 1; i < chunkCmp.Tiles.Length; i++)
        {
            if (Random.Next() % 100 > 95)
            {
                chunkCmp.Tiles[i] = 1;
            }
            else
            {
                chunkCmp.Tiles[i] = 0;
            }
        }
        
        chunkEnt.Set(chunkCmp);
        
        // The chunk's own system will create and manage the physics body
        chunkEnt.Set(new ExternallyManagedPhysicsBody());
        AddChunk(grid, chunkEnt, chunkPosition);
        _chunkSystem.UpdateChunk(chunkEnt);
        return true;

    }
}