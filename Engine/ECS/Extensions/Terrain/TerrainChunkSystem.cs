using System.ComponentModel;
using DefaultEcs;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.Base.EntityWrappers;
using MagicEngine.Engine.ECS.Core.Physics;
using MagicEngine.Engine.ECS.Core.Physics.Bridge.Components;
using MagicEngine.Engine.ECS.Core.Positioning.Components;
using MagicEngine.Engine.ECS.Core.Render.Components;
using MagicEngine.Engine.ECS.Extensions.Terrain.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Dynamics;

namespace MagicEngine.Engine.ECS.Extensions.Terrain;

public class TerrainChunkSystem : EntitySystem
{
    private void UpdateTexture(Entity target)
    {
        if (!target.TryGet<TerrainChunkComponent>(out var chunk))
        {
            return;
        }

        int chunkSize = chunk.Comp.ChunkSize;
        int tileSize = chunk.Comp.TileSize;
        
        int logicalSize = chunkSize * tileSize;

        Texture2D texture = new Texture2D(Graphics, logicalSize, logicalSize);
        
        var pixels = new Color[logicalSize * logicalSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            int pixelX = i % logicalSize;
            int pixelY = i / logicalSize;
            
            int tileX = pixelX / tileSize;
            int tileY = pixelY / tileSize;
            
            int addr = (tileY * chunkSize) + tileX;

            if (chunk.Comp[addr] == 0) // 0 is grass, 1 is air
            {
                pixels[i] = Color.FromNonPremultiplied(57, 189, 92, 255);
            }
            else
            {
                pixels[i] = Color.Transparent;
            }
        }
        texture.SetData(pixels);

        Sprite spriteComp = new Sprite();
        spriteComp.Texture = texture;
        spriteComp.Color = Color.White;
        spriteComp.Anchor = Vector2.Zero;
        target.Set(spriteComp);
    }

    private void UpdateFixtures(Entity target)
    {
        var chunkTopLeft = target.Get<Position>();
        
        if (!target.TryGet<TerrainChunkComponent>(out var chunk))
        {
            return;
        }
        
        if (!target.Has<PhysicsBodyComponent>())
        {
            // this creates the physics body in the top left corner
            var newBody = PhysicsWorld.CreateBody(
                new Vector2(
                    PhysicsSystem.ToPhysics(chunkTopLeft.Value.X),
                    PhysicsSystem.ToPhysics(chunkTopLeft.Value.Y)
                )
            );

            newBody.FixedRotation = true;
            newBody.BodyType = BodyType.Static;

            newBody.Tag = target;
            
            target.Set(new PhysicsBodyComponent
            {
                Body = newBody
            });
            target.Set(new ExternallyManagedPhysicsBody());

            V($"[TerrainChunkSystem] Created physics body for entity {target}");
        }

        if (!chunk.Comp.Parent.TryGet<TerrainComponent>(out var terrain))
        {
            throw new InvalidOperationException(
                "Tried to update fixtures on a chunk attached to a non-terrain. State is likely corrupted, aborting.");
        }
        
        ref var physicsBody = ref target.Get<PhysicsBodyComponent>();
        
        // drop all fixtures
        for (int i = physicsBody.Body.FixtureList.Count - 1; i >= 0; i--)
        {
            physicsBody.Body.Remove(physicsBody.Body.FixtureList[i]);
        }
        
        #region greedy mesh generation
        ref var grid = ref chunk.Comp.Tiles;
        int tilesInChunk = chunk.Comp.ChunkSize;
        int tileSize = chunk.Comp.TileSize;
        
        bool[] needsMesh = new bool[tilesInChunk * tilesInChunk];
        // bool[] items default to false
        bool[] visited = new bool[tilesInChunk * tilesInChunk];

        // copy over the data into a single list. Maybe it'll run faster, who knows?
        // besides, it'll look much cleaner.
        for (int i = 0; i < grid.Length; i++)
        {
            needsMesh[i] = terrain.Comp.Tileset.Tiles[grid[i]].IsCollider;
        }
        
        // actually run the greedy mesh algorithm
        for (int i = 0; i < (tilesInChunk * tilesInChunk); i++)
        {
            if (!needsMesh[i] || visited[i])
                continue;
            
            int startX = i % tilesInChunk;
            int startY = i / tilesInChunk;
            
            // horizontal expand
            int width = 1;
            while ((startX + width < tilesInChunk) && needsMesh[i + width] && !visited[i + width])
            {
                width++;
            }
            
            // vertical expand
            int height = 1;
            bool canExpandDown = true;

            while ((startY + height < tilesInChunk) && canExpandDown)
            {
                int rowStartIndex = i + (height * tilesInChunk);

                for (int xOffset = 0; xOffset < width; xOffset++)
                {
                    int checkIndex = rowStartIndex + xOffset;

                    if (!needsMesh[checkIndex] || visited[checkIndex])
                    {
                        canExpandDown = false;
                        break;
                    }
                }

                if (canExpandDown)
                {
                    height++;
                }
            }
            
            // mark visited
            for (int yOffset = 0; yOffset < height; yOffset++)
            {
                for (int xOffset = 0; xOffset < width; xOffset++)
                {
                    int visitIndex = i + (yOffset * tilesInChunk) + xOffset;
                    visited[visitIndex] = true;
                }
            }
            
            // create fixtures
            // startX and startY is the top left in GRID coordinates
            // width, height is the size in GRID coordinates
            // convert to GAME LOGICAL RELATIVE coordinates (guh)
            // we do this because we need the *OFFSET* from the physics body, which is the top left of the chunk,

            Vector2 fixtureStartRelativePhysical = new Vector2(
                PhysicsSystem.ToPhysics(startX * tileSize),
                PhysicsSystem.ToPhysics(startY * tileSize)
            );
            
            float widthPhysics = PhysicsSystem.ToPhysics(width * tileSize);
            float heightPhysics = PhysicsSystem.ToPhysics(height * tileSize);
            
            // We need to also offset by half the height and width, because we need to provide the
            // center of the fixture
            Vector2 fixtureAdditionalOffsetPhysics = new Vector2(widthPhysics / 2, heightPhysics / 2);
            
            // At this point everything is in physical relative coords
            // we just add up everything to get the final fixture offset
            Vector2 finalFixtureOffsetPhysics = fixtureStartRelativePhysical + fixtureAdditionalOffsetPhysics;
            
            physicsBody.Body.CreateRectangle(
                width: widthPhysics,
                height: heightPhysics,
                density: 1f,
                offset: finalFixtureOffsetPhysics
            );
        }
        #endregion
    }
    
    public void UpdateChunk(Entity target)
    {
        UpdateTexture(target);
        UpdateFixtures(target);
    }
}