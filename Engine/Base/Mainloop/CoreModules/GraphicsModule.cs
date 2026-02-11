// Tightly coupled shitcode that is as-is as a first step in a nightmare refactor
// function dependencies and stuff will need a proper refactor to decouple at some point

using MagicEngine.Engine.Base.Debug;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.Base.Scene;
using MagicEngine.Engine.Base.Shaders.PostProcessing;
using MagicEngine.Engine.Base.Utilities;
using MagicEngine.Engine.ECS.Core.Camera;
using MagicEngine.Engine.ECS.Core.Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MagicEngine.Engine.Base.CoreModules;

public class EngineGraphicsModule : IEngineGraphicsModule
{
    public GraphicsManager GraphicsManager { get; private set; }
    public PostProcessingManager PostProcessingManager { get; private set; }
    public Texture2D WhitePixel  { get; private set; }
    public LogManager LogManager { get; private set; }
    public GameWindow Window { get; private set; }
    
    private Texture2D? textureWithPP;
    private Texture2D? finalTextureWithPP;

    public EngineGraphicsModule(GraphicsManager graphicsManager, GraphicsDevice graphicsDevice, GameWindow gameWindow, LogManager logManager, PostProcessingManager postProcessingManager)
    {
        GraphicsManager = graphicsManager;
        LogManager = logManager;
        Window = gameWindow;
        
        LogManager.Log("Initializing graphics module...");
        GraphicsManager.Initialize();
        GraphicsManager.InitializeRenderTargets(GraphicsManager.Graphics.GraphicsDevice);
        Window.IsBorderless = true;
        
       PostProcessingManager = postProcessingManager;
    }

    public void LoadContent()
    {
        LogManager.Log("Startup: Sprite batch setup", LogLevel.VerboseExtra);
        GraphicsManager.SpriteBatch = new SpriteBatch(GraphicsManager.Graphics.GraphicsDevice);
        WhitePixel = new Texture2D(GraphicsManager.Graphics.GraphicsDevice, 1, 1);
        WhitePixel.SetData(new[] { Color.White });
    }

    public void Draw(bool isCrashing, double gameTimeTemp, GameTime gameTime, float timeAccumulator, float fixedTimeStep, CameraSystem cameraSystem, SystemManager systemManager, bool debugRender, SceneManager sceneManager)
    {
        if (!isCrashing)
        {
            bool t = RenderStepLowResSpriteDraw(
                gameTimeTemp, gameTime, timeAccumulator, 
                fixedTimeStep, cameraSystem, systemManager, 
                debugRender, sceneManager
            );

            // todo restore this functionality during coupling refactor
            //if (!t)
            //{
            //    debugRender = false;
            //}
                
        }
        else
        {
            RenderStepLowResSpriteDrawCrashMode();
        }
        RenderStepUpscaleToHighRes(cameraSystem);
    }

    public bool RenderStepLowResSpriteDraw(double gameTimeTemp, GameTime gameTime, float timeAccumulator, float fixedTimeStep, CameraSystem cameraSystem, SystemManager systemManager, bool debugRender, SceneManager sceneManager)
    {
        if (GraphicsManager.Graphics.GraphicsDevice.BlendState == null) 
            GraphicsManager.Graphics.GraphicsDevice.BlendState = BlendState.Opaque;
    
        if (GraphicsManager.Graphics.GraphicsDevice.DepthStencilState == null) 
            GraphicsManager.Graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
    
        if (GraphicsManager.Graphics.GraphicsDevice.RasterizerState == null) 
            GraphicsManager.Graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
    
        if (GraphicsManager.Graphics.GraphicsDevice.SamplerStates[0] == null) 
            GraphicsManager.Graphics.GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
        
        GraphicsManager.Graphics.GraphicsDevice.SetRenderTarget(GraphicsManager.RenderTarget);
        GraphicsManager.Graphics.GraphicsDevice.Clear(ColorOperations.ToLinear(Color.Black));

        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
    
        // camera stuff todo refactor
        Vector2 cameraIntPosition = new Vector2((int)Math.Round(cameraSystem.Position.X), (int)Math.Round(cameraSystem.Position.Y));
        Matrix transform = Matrix.CreateTranslation(-cameraIntPosition.X, -cameraIntPosition.Y, 0) *
                           Matrix.CreateTranslation(
                               GraphicsManager.Screen.VirtualWidth / 2f + GraphicsManager.Screen.Padding, 
                               GraphicsManager.Screen.VirtualHeight / 2f + GraphicsManager.Screen.Padding, 0
                               );
    
        GraphicsManager.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: transform);
        
        // Create Draw Timing
        var drawTiming = new Timing(deltaTime, (float)(timeAccumulator / fixedTimeStep), gameTimeTemp);
        systemManager.RunDraw(drawTiming, GraphicsManager.SpriteBatch, transform);
        
        GraphicsManager.SpriteBatch.End();

        textureWithPP = PostProcessingManager.ApplyEffects(
            EffectType.TexelLayer,
            GraphicsManager.SpriteBatch, 
            GraphicsManager.RenderTarget,
            GraphicsManager.RenderTarget,
            GraphicsManager.ShadowTarget);
        
        

        // render debug hitboxes
        if (debugRender)
        {
            Matrix projection = Matrix.CreateOrthographicOffCenter(
                0, 
                GraphicsManager.RenderTarget.Width, 
                GraphicsManager.RenderTarget.Height, 
                0, 
                0, 
                1);
        
            Matrix view = Matrix.CreateScale(PhysicsConstants.PixelsPerMeter) * transform;
        
            try
            {
                sceneManager.GetScene().AttachedSystems.DebugView.RenderDebugData(ref projection, ref view);
            }
            catch (Exception e)
            {
                LogManager.Release($"DebugView render failed: {e.Message}", "--MAINLOOP--");
                // DebugRender = false; coupling temp
                return false;
            }
        }

        return true;
    }

    public void RenderStepLowResSpriteDrawCrashMode()
    {
        textureWithPP = GraphicsManager.RenderTarget;
    }

    public void RenderStepUpscaleToHighRes(CameraSystem cameraSystem)
    {
        if (GraphicsManager.Graphics.GraphicsDevice.BlendState == null) 
            GraphicsManager.Graphics.GraphicsDevice.BlendState = BlendState.Opaque;
    
        if (GraphicsManager.Graphics.GraphicsDevice.DepthStencilState == null) 
            GraphicsManager.Graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
    
        if (GraphicsManager.Graphics.GraphicsDevice.RasterizerState == null) 
            GraphicsManager.Graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
    
        if (GraphicsManager.Graphics.GraphicsDevice.SamplerStates[0] == null) 
            GraphicsManager.Graphics.GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
        
        GraphicsManager.Graphics.GraphicsDevice.SetRenderTarget(GraphicsManager.ScreenTarget);
        GraphicsManager.Graphics.GraphicsDevice.Clear(Color.Black);

        // no clue what I'm doing
        // but we are trying to upscale with subpixel movement smoothing
        // the camera is only offset by integer values, but we move the final rendered image
        // by the remainder of the floats.
        float screenWidth = GraphicsManager.Graphics.GraphicsDevice.Viewport.Width;
        float screenHeight = GraphicsManager.Graphics.GraphicsDevice.Viewport.Height;
        float scaleX = screenWidth / GraphicsManager.Screen.VirtualWidth;
        float scaleY = screenHeight / GraphicsManager.Screen.VirtualHeight;
        float scale = Math.Min(scaleX, scaleY);
        
        int scaledWidth = (int)(GraphicsManager.Screen.VirtualWidth * scale);
        int scaledHeight = (int)(GraphicsManager.Screen.VirtualHeight * scale);
        int posX = (int)((screenWidth - scaledWidth) / 2);
        int posY = (int)((screenHeight - scaledHeight) / 2);
        
        Vector2 subPixel = new Vector2((int)Math.Round(cameraSystem.Position.X), (int)Math.Round(cameraSystem.Position.Y)) - cameraSystem.Position;
        posX += (int)(subPixel.X * scale);
        posY += (int)(subPixel.Y * scale);

        Rectangle finalDestination = new Rectangle(posX, posY, scaledWidth, scaledHeight);
        
        // Draw low-res texture with texel effects into high res
        GraphicsManager.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        GraphicsManager.SpriteBatch.Draw(textureWithPP, finalDestination, new Rectangle(
            GraphicsManager.Screen.Padding, GraphicsManager.Screen.Padding, GraphicsManager.Screen.VirtualWidth, GraphicsManager.Screen.VirtualHeight
            ), Color.White);
        GraphicsManager.SpriteBatch.End();
        
        //// APPLY TEXEL PP EFFECTS TO HIGH RES TEXTURE
        finalTextureWithPP = PostProcessingManager.ApplyEffects(
            EffectType.PixelLayer,
            GraphicsManager.SpriteBatch, 
            GraphicsManager.ScreenTarget,
            GraphicsManager.ScreenTarget,
            GraphicsManager.ShadowScreenTarget
        );
        
        GraphicsManager.Graphics.GraphicsDevice.SetRenderTarget(null);
        GraphicsManager.Graphics.GraphicsDevice.Clear(Color.Black);

        GraphicsManager.SpriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        GraphicsManager.SpriteBatch.Draw(finalTextureWithPP, finalDestination, Color.White);
        GraphicsManager.SpriteBatch.End();
        
        GraphicsManager.Graphics.GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsManager.Graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsManager.Graphics.GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
    }

    public void DrawCursor(Texture2D cursorTexture, Vector2 cursorHotspot)
    {
        GraphicsManager.SpriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        if (cursorTexture != null)
        {
            var mouseState = Mouse.GetState();
            Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);
            float cursorScale = 0.1f; 

            GraphicsManager.SpriteBatch.Draw(
                cursorTexture,
                mousePos,
                null,
                Color.White,
                0f,
                cursorHotspot,
                cursorScale,
                SpriteEffects.None,
                0f
            );
        }
        GraphicsManager.SpriteBatch.End();
    }
}