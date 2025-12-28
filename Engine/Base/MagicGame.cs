using System;
using System.Collections.Generic;
using DefaultEcs;
using ImGuiNET;
using MagicThing.Engine.Base.Debug;
using MagicThing.Engine.Base.Debug.Commands;
using MagicThing.Engine.Base.Debug.UI;
using MagicThing.Engine.Base.EntitySystem;
using MagicThing.Engine.Base.Events;
using MagicThing.Engine.Base.PrototypeComponentSystem;
using MagicThing.Engine.Base.Scene;
using MagicThing.Engine.Base.Shaders.PostProcessing;
using MagicThing.Engine.Base.Utilities;
using MagicThing.Engine.ECS.Core.Camera;
using MagicThing.Engine.ECS.Core.Events;
using MagicThing.Engine.ECS.Core.Input;
using MagicThing.Engine.ECS.Core.Physics;
using MagicThing.Engine.ECS.Core.Physics.Behavior;
using MagicThing.Engine.ECS.Core.Physics.Bridge;
using MagicThing.Engine.ECS.Core.Positioning;
using MagicThing.Engine.ECS.Core.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using nkast.Aether.Physics2D.Controllers;

namespace MagicThing.Engine.Base;

public abstract class MagicGame : Game
{
    #region Graphics related
    protected GraphicsManager GraphicsManager;
    protected PostProcessingManager PostProcessingManager;
    
    public static Texture2D WhitePixel;
    #endregion
    
    #region NEW scene management systems
    protected SceneManager SceneManager;
    #endregion
    
    #region ECS related
    protected SystemManager SystemManager;
    protected PrototypeManager PrototypeManager;
    #endregion
    
    protected LogManager LogManager;
    
    #region Low level game systems - Camera
    protected CameraSystem CameraSystem;
    #endregion
    
    #region ImGui debug overlays
    protected ImGuiRenderer ImGuiRenderer;
    protected bool DebugActive = false;
    protected readonly ComponentViewerPanel ComponentViewerPanel = new();
    protected readonly List<InspectorWindowState> InspectorWindow = new();
    protected CrashInspectorPanel CrashInspectorPanel;
    
    protected class InspectorWindowState
    {
        public Entity TargetEntity;
        public bool IsOpen = true;
    }
    
    protected CommandManager _commandManager;
    protected DebugConsoleWindow _consoleWindow;
    protected ConsoleInterceptor _consoleInterceptor;
    protected PostProcessDebugOverlay _postProcessDebugOverlay;
    #endregion
    
    #region State
    protected bool DebugRender;
    protected int DebugRenderCooldown;
    protected const float FixedTimeStep = 1f / 60f;
    protected float TimeAccumulator = 0f;
    protected Random _random;
    protected bool _consoleActive = false;

    protected double GameTime = 0;
    
    // Profiling
    private double _fps;
    private double _frameTime;
    private const float Smoothing = 0.95f;
    protected readonly DiagnosticsPanel DiagnosticsPanel = new();
    #endregion
    
    protected MagicGame()
    {
        GraphicsManager = new GraphicsManager(new GraphicsDeviceManager(this), 640, 360, 4);
        
        Content.RootDirectory = "Content";
    }

    protected override void Initialize()
    {
        // --- BORDERLESS FULLSCREEN SETUP ---
        GraphicsManager.Graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        GraphicsManager.Graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        GraphicsManager.Graphics.HardwareModeSwitch = false; // Important for fast switching
        GraphicsManager.Graphics.IsFullScreen = true;
        Window.IsBorderless = true;
        GraphicsManager.Graphics.SynchronizeWithVerticalRetrace = true;
        GraphicsManager.Graphics.GraphicsProfile = GraphicsProfile.HiDef;
        GraphicsManager.Graphics.ApplyChanges();
        
        _consoleInterceptor = new ConsoleInterceptor(Console.Out);
        Console.SetOut(_consoleInterceptor);
        Console.WriteLine("[Console] Output redirected to debug window.");

        LogManager = new LogManager();
        LogManager.LogMode = LogLevel.VerboseExtra;
        
        _random = new Random();
        SceneManager = new SceneManager(GraphicsDevice, Content);
        
        SceneManager.RegsiterScene(new Scene.Scene(
            new SceneCreationResources(GraphicsDevice, Content),
            "BaseEngineScene",
            new World(),
            new EventManager(),
            new nkast.Aether.Physics2D.Dynamics.World(new Vector2(0, 0))
        ));

        LogManager.Log("Startup: PrototypeManager", LogLevel.VerboseExtra);
        PrototypeManager = new PrototypeManager(SceneManager, Content);
        CameraSystem = new CameraSystem();
        
        LogManager.Log("Startup: SystemManager", LogLevel.VerboseExtra);
        SystemManager = new SystemManager(SceneManager, _random, PrototypeManager, CameraSystem, LogManager, Content);
        SystemManager.Initialize();
        
        LogManager.Verbose("Startup: Initializing Post Processing Manager");
        PostProcessingManager = new PostProcessingManager();
        
        LogManager.Log("Startup: CommandManager", LogLevel.VerboseExtra);
        _commandManager = new CommandManager(SystemManager, SceneManager, PrototypeManager, _random, CameraSystem, LogManager, PostProcessingManager);
        _commandManager.Initialize();
        
        // This action is guaranteed to crash if ran before SystemManager is initialized.
        // Anything that depends on an initialized scene requires this, though.
        SceneManager.FirstLoadSceneUnsafe("BaseEngineScene");
        
        LogManager.Log("Startup: DebugConsoleWindow", LogLevel.VerboseExtra);
        _consoleWindow = new DebugConsoleWindow(_commandManager, _consoleInterceptor);
        _postProcessDebugOverlay = new PostProcessDebugOverlay(PostProcessingManager);
        
        LogManager.Log("Startup: Debug view content load", LogLevel.VerboseExtra);
        SceneManager.GetScene().AttachedSystems.DebugView.LoadContent(GraphicsDevice, Content);
        
        DebugRender = false;
        DebugRenderCooldown = 10;

        LogManager.Log("Startup: ImGuiRender", LogLevel.VerboseExtra);
        ImGuiRenderer = new ImGuiRenderer(this);
        ImGuiRenderer.RebuildFontAtlas();
        

        CrashInspectorPanel = new CrashInspectorPanel(this);
        

        // --- RENDER TARGET SETUP ---
        // Create the RenderTarget2D with our virtual resolution
        LogManager.Log("Startup: Creating render target", LogLevel.VerboseExtra);
        
        // We use HalfVector4 (R,G,B,A 16-bit float) for HDR support to allow values > 1.0
        GraphicsManager.RenderTarget = new RenderTarget2D(
            GraphicsDevice,
            GraphicsManager.Screen.VirtualWidth + GraphicsManager.Screen.Padding * 2,
            GraphicsManager.Screen.VirtualHeight + GraphicsManager.Screen.Padding * 2,
            false,
            SurfaceFormat.HalfVector4,
            DepthFormat.None);
        
        LogManager.Log("Startup: Creating shadow target", LogLevel.VerboseExtra);
        GraphicsManager.ShadowTarget = new RenderTarget2D(
            GraphicsDevice,
            GraphicsManager.Screen.VirtualWidth + GraphicsManager.Screen.Padding * 2,
            GraphicsManager.Screen.VirtualHeight + GraphicsManager.Screen.Padding * 2,
            false,
            SurfaceFormat.HalfVector4,
            DepthFormat.None);
        
        LogManager.Log("Startup: Creating High-Res render target", LogLevel.VerboseExtra);
        GraphicsManager.ScreenTarget = new RenderTarget2D(GraphicsDevice, 
            GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height,
            false,
            SurfaceFormat.HalfVector4,
            DepthFormat.None);
        
        LogManager.Log("Startup: Creating High-Res shadow render target", LogLevel.VerboseExtra);
        GraphicsManager.ShadowScreenTarget = new RenderTarget2D(GraphicsDevice, 
            GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height,
            false,
            SurfaceFormat.HalfVector4,
            DepthFormat.None);
        
        
        LogManager.Log("Startup: Calling user game system hooks", LogLevel.Verbose);
        
        RegisterGameSystemsHook();
        
        LogManager.Release("Startup: Engine initialized.");
        base.Initialize();
    }
    
    protected virtual void RegisterGameSystemsHook() { }
    
    protected override void LoadContent()
    {
        LogManager.Log("Startup: Sprite batch setup", LogLevel.VerboseExtra);
        GraphicsManager.SpriteBatch = new SpriteBatch(GraphicsDevice);
        
        WhitePixel = new Texture2D(GraphicsDevice, 1, 1);
        WhitePixel.SetData(new[] { Color.White });
        
        LogManager.Log("Startup: Prototype content load", LogLevel.VerboseExtra);
        PrototypeManager.Initialize();
        
        LogManager.Log("Startup: Calling user content load hooks", LogLevel.Verbose);
        
        LoadGameContent();
        
        LogManager.Release("Startup: All content loaded.");
    }
    
    protected abstract void LoadGameContent();

    protected virtual void RunPreFixedUpdateContent(Timing timing) {}
    protected virtual void RunFixedUpdateContent(Timing timing) {}
    protected virtual void RunPostFixedUpdateContent(Timing timing) {}
    protected virtual void RunPausedFrameUpdate(Timing timing) {}
    
    protected override void Update(GameTime gameTime)
    {
        try
        {
            #region Update loop
            if (CrashInspectorPanel.IsActive)
            {
                base.Update(gameTime);
                return;
            }
            
            // ========= Debug stuff start
            if (Keyboard.GetState().IsKeyDown(Keys.F1) && DebugRenderCooldown <= 0)
            {
                DebugRender  = !DebugRender;
                DebugActive = true;
                DebugRenderCooldown = 20;
            }
            
            if (Keyboard.GetState().IsKeyDown(Keys.F2) && DebugRenderCooldown <= 0)
            {
                _consoleActive = !_consoleActive;
                DebugActive = true;
                DebugRenderCooldown = 20;
            }
            
            if (Keyboard.GetState().IsKeyDown(Keys.F3) && DebugRenderCooldown <= 0)
            {
                PostProcessingManager.Enabled = !PostProcessingManager.Enabled;
                DebugRenderCooldown = 20;
            }
            DebugRenderCooldown--;
            // ========= Debug stuff end
            
            var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            GameTime += deltaTime;
            
            // FPS & FrameTime Calculation
            if (deltaTime > 0.000001f)
            {
                double currentFps = 1.0 / deltaTime;
                _fps = (_fps * Smoothing) + (currentFps * (1.0 - Smoothing));
            }
            _frameTime = deltaTime * 1000.0;
            
            var preLoopTiming = new Timing(deltaTime, 0f, GameTime);
            
            SystemManager.RunFrameStart(preLoopTiming);
            SystemManager.RunTransientUpdate(preLoopTiming);
            
            // TODO: this is absolute fucking shitcode, refactor this
            if (SystemManager.GetSystem<SessionManager>().IsPaused)
            {
                SystemManager.RunPausedUpdate(preLoopTiming);
                RunPausedFrameUpdate(preLoopTiming);
            }
            else
            {
                RunPreFixedUpdateContent(preLoopTiming);
                TimeAccumulator += deltaTime * SystemManager.GetSystem<SessionManager>().GameSpeed;
                var fixedTiming = new Timing(FixedTimeStep, 1.0f, GameTime);
    
            
                while (TimeAccumulator >= FixedTimeStep)
                {
                    SystemManager.RunFixedUpdatePrePhysics(fixedTiming);
                
                    // Engine housekeeping
                    SystemManager.GetSystem<ProcessPositionRequestSystem>().ManualUpdate();
                
                    // Physics bridge and the system itself ---
                    SystemManager.Profiler.Profile("Physics: BodyCreation", () => SceneManager.GetScene().AttachedSystems.BodyCreationSystem.Update(FixedTimeStep));
                    SystemManager.Profiler.Profile("Physics: PreSync", () => SceneManager.GetScene().AttachedSystems.PrePhysicsSyncSystem.Update(FixedTimeStep));
                
                    SystemManager.Profiler.Profile("Physics: Simulation", () => 
                    {
                        SceneManager.GetScene().PhysicsWorld.Step(FixedTimeStep);
                        SceneManager.GetScene().PhysicsWorld.ClearForces();
                    });
                
                    SystemManager.Profiler.Profile("Physics: PostSync", () => SceneManager.GetScene().AttachedSystems.PostPhysicsSyncSystem.Update(FixedTimeStep));
                    SystemManager.Profiler.Profile("Physics: BodyDeletion", () => SceneManager.GetScene().AttachedSystems.BodyDeletionSystem.Update(FixedTimeStep));
                    
                    // WeldManagerSystem also runs in cleanup but let's run it here to ensure consistent state
                    SystemManager.Profiler.Profile("Physics: WeldManager", () => SystemManager.GetSystem<WeldManagerSystem>().Update(fixedTiming));
                    // ----------------------------------------
                
                    SystemManager.RunFixedUpdatePostPhysics(fixedTiming);
                    
                    RunFixedUpdateContent(fixedTiming);
                    TimeAccumulator -= FixedTimeStep;
                }
            
                var alpha = (float)(TimeAccumulator / FixedTimeStep); 
                var postLoopTiming = new Timing(deltaTime, alpha, GameTime);
            
                SystemManager.RunFrameLateUpdate(postLoopTiming);
                
                RunPostFixedUpdateContent(postLoopTiming);
            
                // (Cleanup, PreRender)
                SystemManager.RunFrameEnd(postLoopTiming);
            }
            
            // We can imagine Draw() being ran here.
            #endregion
        }
        catch (Exception e)
        {
            if (!CrashInspectorPanel.IsActive)
            {
                Console.WriteLine(e);
                EnterCrashInspector(e);
            }
            else
            {
                // The inspector's update code crashed. Let it terminate.
                throw;
            }
        }
        
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        try
        {
            Texture2D textureWithPP;
            Texture2D finalTextureWithPP;
                
            #region GAME draw loop
            if (!CrashInspectorPanel.IsActive)
            {
                GraphicsDevice.SetRenderTarget(GraphicsManager.RenderTarget);
                GraphicsDevice.Clear(ColorOperations.ToLinear(Color.Black));

                var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
                // camera stuff todo refactor
                Vector2 cameraIntPosition = new Vector2((int)Math.Round(CameraSystem.Position.X), (int)Math.Round(CameraSystem.Position.Y));
                Matrix transform = Matrix.CreateTranslation(-cameraIntPosition.X, -cameraIntPosition.Y, 0) *
                                   Matrix.CreateTranslation(
                                       GraphicsManager.Screen.VirtualWidth / 2f + GraphicsManager.Screen.Padding, 
                                       GraphicsManager.Screen.VirtualHeight / 2f + GraphicsManager.Screen.Padding, 0
                                       );
            
                GraphicsManager.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: transform);
                
                // Create Draw Timing
                var drawTiming = new Timing(deltaTime, (float)(TimeAccumulator / FixedTimeStep), GameTime);
                SystemManager.RunDraw(drawTiming, GraphicsManager.SpriteBatch, transform);
                
                GraphicsManager.SpriteBatch.End();

                textureWithPP = PostProcessingManager.ApplyEffects(
                    EffectType.TexelLayer,
                    GraphicsManager.SpriteBatch, 
                    GraphicsManager.RenderTarget,
                    GraphicsManager.RenderTarget,
                    GraphicsManager.ShadowTarget);

                if (DebugRender)
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
                        SceneManager.GetScene().AttachedSystems.DebugView.RenderDebugData(ref projection, ref view);
                    }
                    catch (Exception e)
                    {
                        LogManager.Release($"DebugView render failed: {e.Message}", "--MAINLOOP--");
                        DebugRender = false;
                    }
                }
            }
            else
            {
                textureWithPP = GraphicsManager.RenderTarget;
            }
            
            GraphicsDevice.SetRenderTarget(GraphicsManager.ScreenTarget);
            GraphicsDevice.Clear(Color.Black);

            // no clue what I'm doing
            // but we are trying to upscale with subpixel movement smoothing
            // the camera is only offset by integer values, but we move the final rendered image
            // by the remainder of the floats.
            float screenWidth = GraphicsDevice.Viewport.Width;
            float screenHeight = GraphicsDevice.Viewport.Height;
            float scaleX = screenWidth / GraphicsManager.Screen.VirtualWidth;
            float scaleY = screenHeight / GraphicsManager.Screen.VirtualHeight;
            float scale = Math.Min(scaleX, scaleY);
            
            int scaledWidth = (int)(GraphicsManager.Screen.VirtualWidth * scale);
            int scaledHeight = (int)(GraphicsManager.Screen.VirtualHeight * scale);
            int posX = (int)((screenWidth - scaledWidth) / 2);
            int posY = (int)((screenHeight - scaledHeight) / 2);
            
            Vector2 subPixel = new Vector2((int)Math.Round(CameraSystem.Position.X), (int)Math.Round(CameraSystem.Position.Y)) - CameraSystem.Position;
            posX += (int)(subPixel.X * scale);
            posY += (int)(subPixel.Y * scale);

            Rectangle finalDestination = new Rectangle(posX, posY, scaledWidth, scaledHeight); ;
            
            // Draw low-res texture with texel effects into high res
            GraphicsManager.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            GraphicsManager.SpriteBatch.Draw(textureWithPP, finalDestination, new Rectangle(
                GraphicsManager.Screen.Padding, GraphicsManager.Screen.Padding, GraphicsManager.Screen.VirtualWidth, GraphicsManager.Screen.VirtualHeight
                ), Color.White);
            GraphicsManager.SpriteBatch.End();
            
            finalTextureWithPP = PostProcessingManager.ApplyEffects(
                EffectType.PixelLayer,
                GraphicsManager.SpriteBatch, 
                GraphicsManager.ScreenTarget,
                GraphicsManager.ScreenTarget,
                GraphicsManager.ShadowScreenTarget
            );
            
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            GraphicsManager.SpriteBatch.Begin(samplerState: SamplerState.LinearClamp);
            GraphicsManager.SpriteBatch.Draw(finalTextureWithPP, finalDestination, Color.White);
            GraphicsManager.SpriteBatch.End();
            #endregion
            
            #region DEBUG render loop
            if (DebugActive)
            {
                // Reset render states to ensure ImGui has a clean slate
                GraphicsDevice.BlendState = BlendState.Opaque;
                GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                GraphicsDevice.RasterizerState = RasterizerState.CullNone;
                GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
                
                ImGuiRenderer.BeforeLayout(gameTime);

                try
                {
                    var inputManager = SystemManager.GetSystem<InputManagerSystem>();
                    var io = ImGui.GetIO();

                    inputManager.IsInputDisabled = CrashInspectorPanel.IsActive || io.WantCaptureKeyboard;

                    ImGui.GetForegroundDrawList().AddCircleFilled(io.MousePos, 5f, 0xFF0000FF);

                    var sceneGraphPanel = SceneManager.GetScene().AttachedSystems.SceneGraphPanel;
                    sceneGraphPanel.Draw(ref DebugActive);

                    if (sceneGraphPanel.EntityToInspect.HasValue)
                    {
                        InspectorWindow.Add(new InspectorWindowState
                        {
                            TargetEntity = sceneGraphPanel.EntityToInspect.Value
                        });
                    }

                    foreach (var windowState in InspectorWindow)
                    {
                        ComponentViewerPanel.Draw(
                            windowState.TargetEntity,
                            ref windowState.IsOpen,
                            windowState.GetHashCode()
                        );
                    }

                    InspectorWindow.RemoveAll(window => !window.IsOpen);
                    _consoleWindow.Draw(ref _consoleActive);
                    _postProcessDebugOverlay.Draw();
                    CrashInspectorPanel.Draw(gameTime);
                    
                    DiagnosticsPanel.Draw(ref DebugActive, GraphicsDevice, _fps, _frameTime, SystemManager.Profiler);
                }
                finally
                {
                    ImGuiRenderer.AfterLayout();
                }
            }
            #endregion
        }
        catch (Exception e)
        {
            if (!CrashInspectorPanel.IsActive)
            {
                Console.WriteLine(e);
                //GraphicsDevice.SetRenderTarget(null);
                EnterCrashInspector(e);
            }
            else
            {
                // The inspector's drawing code crashed. Let it terminate.
                throw;
            }
        }

        base.Draw(gameTime);
    }
    
    protected override void UnloadContent()
    {
        WhitePixel?.Dispose();
        WhitePixel = null;
        // ...
        base.UnloadContent();
    }

    protected void EnterCrashInspector(Exception e)
    {
        LogManager.Release("==== BEGIN FATAL CRASH ====");
        CrashInspectorPanel.Activate(e);
        DebugActive = true;
        LogManager.Debug("Entering crash inspector.");
    }
}
