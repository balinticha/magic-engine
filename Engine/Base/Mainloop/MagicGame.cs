using DefaultEcs;
using ImGuiNET;
using MagicEngine.Engine.Base.CoreModules;
using MagicEngine.Engine.Base.Debug;
using MagicEngine.Engine.Base.Debug.Commands;
using MagicEngine.Engine.Base.Debug.UI;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.Base.Events;
using MagicEngine.Engine.Base.PrototypeComponentSystem;
using MagicEngine.Engine.Base.Scene;
using MagicEngine.Engine.Base.Shaders.PostProcessing;
using MagicEngine.Engine.Base.Utilities;
using MagicEngine.Engine.ECS.Core.Camera;
using MagicEngine.Engine.ECS.Core.Input;
using MagicEngine.Engine.ECS.Core.Physics;
using MagicEngine.Engine.ECS.Core.Physics.Behavior;
using MagicEngine.Engine.ECS.Core.Positioning;
using MagicEngine.Engine.ECS.Core.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;

namespace MagicEngine.Engine.Base;

public abstract class MagicGame : Game
{
    #region New modules

    private GraphicsManager _gp;
    public EngineGraphicsModule EngineGraphicsModule;
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

    protected double GameTimeTemp = 0;
    
    // Profiling
    private double _fps;
    private double _frameTime;
    private const float Smoothing = 0.95f;
    protected readonly DiagnosticsPanel DiagnosticsPanel = new();
    #endregion

    #region  UI
    protected GumService GumUI => GumService.Default;
    
    protected virtual void InitializeUI() {}
    protected virtual void UpdateUI(GameTime gameTime) { }
    protected virtual void DrawUI() {}
    
    // TODO should be exposed to clients eventually
    protected Texture2D CursorTexture;
    protected Vector2 CursorHotspot = Vector2.Zero;
    
    #endregion
    
    protected MagicGame()
    {
        _gp = new GraphicsManager(new GraphicsDeviceManager(this), 640, 360, 4);
        Content.RootDirectory = "Content";
    }

    protected override void Initialize()
    {
        _random = new Random();
        
        // Console and logging
        _consoleInterceptor = new ConsoleInterceptor(Console.Out);
        Console.SetOut(_consoleInterceptor);
        Console.WriteLine("[Console] Output redirected to debug window.");
        LogManager = new LogManager();
        LogManager.LogMode = LogLevel.VerboseExtra;
        
        EngineGraphicsModule = new EngineGraphicsModule(
            _gp,
            GraphicsDevice,
            Window,
            LogManager,
            new PostProcessingManager()
        );
        
        InitializeUI();
        
        // Prototypes and core systems
        LogManager.Log("Startup: PrototypeManager", LogLevel.VerboseExtra);
        SceneManager = new SceneManager(GraphicsDevice, Content);
        PrototypeManager = new PrototypeManager(SceneManager, Content);
        CameraSystem = new CameraSystem();
        
        LogManager.Log("Startup: SystemManager", LogLevel.VerboseExtra);
        SystemManager = new SystemManager(SceneManager, _random, PrototypeManager, CameraSystem, LogManager, Content);
        SystemManager.Initialize();
        
        // Scene registration
        SceneManager.RegsiterScene(new Scene.Scene(
            new SceneCreationResources(GraphicsDevice, Content),
            "BaseEngineScene",
            new World(),
            new EventManager(),
            new nkast.Aether.Physics2D.Dynamics.World(new Vector2(0, 0))
        ));
        SceneManager.FirstLoadSceneUnsafe("BaseEngineScene");
        
        // Commands
        LogManager.Log("Startup: CommandManager", LogLevel.VerboseExtra);
        _commandManager = new CommandManager(SystemManager, SceneManager, PrototypeManager, _random, CameraSystem, LogManager, EngineGraphicsModule.PostProcessingManager);
        _commandManager.Initialize();
        
        LogManager.Log("Startup: DebugConsoleWindow", LogLevel.VerboseExtra);
        _consoleWindow = new DebugConsoleWindow(_commandManager, _consoleInterceptor);
        _postProcessDebugOverlay = new PostProcessDebugOverlay(EngineGraphicsModule.PostProcessingManager);
        
        LogManager.Log("Startup: Debug view content load", LogLevel.VerboseExtra);
        SceneManager.GetScene().AttachedSystems.DebugView.LoadContent(GraphicsDevice, Content);
        
        DebugRender = false;
        DebugRenderCooldown = 10;

        LogManager.Log("Startup: ImGuiRender", LogLevel.VerboseExtra);
        ImGuiRenderer = new ImGuiRenderer(this);
        ImGuiRenderer.RebuildFontAtlas();

        CrashInspectorPanel = new CrashInspectorPanel(this);
        
        RegisterGameSystemsHook();
        
        LogManager.Release("Startup: Engine initialized.");
        base.Initialize();
    }
    
    protected virtual void RegisterGameSystemsHook() { }
    
    protected override void LoadContent()
    {
        EngineGraphicsModule.LoadContent();
        
        LogManager.Log("Startup: Prototype content load", LogLevel.VerboseExtra);
        PrototypeManager.Initialize();
        
        LogManager.Log("Startup: Calling user content load hooks", LogLevel.Verbose);
        
        CursorTexture = Content.Load<Texture2D>("Cursor");
        
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
            
            UpdateUI(gameTime);
            
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
                EngineGraphicsModule.PostProcessingManager.Enabled = !EngineGraphicsModule.PostProcessingManager.Enabled;
                DebugRenderCooldown = 20;
            }
            DebugRenderCooldown--;
            // ========= Debug stuff end
            
            var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            GameTimeTemp += deltaTime;
            
            // FPS & FrameTime Calculation
            if (deltaTime > 0.000001f)
            {
                double currentFps = 1.0 / deltaTime;
                _fps = (_fps * Smoothing) + (currentFps * (1.0 - Smoothing));
            }
            _frameTime = deltaTime * 1000.0;
            
            var preLoopTiming = new Timing(deltaTime, 0f, GameTimeTemp);
            
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
                var fixedTiming = new Timing(FixedTimeStep, 1.0f, GameTimeTemp);
                
                while (TimeAccumulator >= FixedTimeStep)
                {
                    SystemManager.RunFixedUpdatePrePhysics(fixedTiming);
                
                    // Engine housekeeping
                    SystemManager.GetSystem<ProcessPositionRequestSystem>().ManualUpdate();
                
                    // Physics bridge and the system itself ---
                    using (SystemManager.Profiler.Profile("Physics: BodyCreation"))
                    {
                        SceneManager.GetScene().AttachedSystems.BodyCreationSystem.Update(FixedTimeStep);
                    }
                    using (SystemManager.Profiler.Profile("Physics: PreSync"))
                    {
                        SceneManager.GetScene().AttachedSystems.PrePhysicsSyncSystem.Update(FixedTimeStep);
                    }
                    using (SystemManager.Profiler.Profile("Physics: Simulation"))
                    {
                        SceneManager.GetScene().PhysicsWorld.Step(FixedTimeStep);
                        SceneManager.GetScene().PhysicsWorld.ClearForces();
                    }
                    using (SystemManager.Profiler.Profile("Physics: PostSync"))
                    {
                        SceneManager.GetScene().AttachedSystems.PostPhysicsSyncSystem.Update(FixedTimeStep);
                    }
                    using (SystemManager.Profiler.Profile("Physics: BodyDeletion"))
                    {
                        SceneManager.GetScene().AttachedSystems.BodyDeletionSystem.Update(FixedTimeStep);
                    }
                    using (SystemManager.Profiler.Profile("Physics: WeldManager"))
                    {
                        SystemManager.GetSystem<WeldManagerSystem>().Update(fixedTiming);
                    }
                    // ----------------------------------------
                
                    SystemManager.RunFixedUpdatePostPhysics(fixedTiming);
                    
                    RunFixedUpdateContent(fixedTiming);
                    TimeAccumulator -= FixedTimeStep;
                }
            
                var alpha = (float)(TimeAccumulator / FixedTimeStep); 
                var postLoopTiming = new Timing(deltaTime, alpha, GameTimeTemp);
            
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
            #region GAME draw loop
            EngineGraphicsModule.Draw(
                CrashInspectorPanel.IsActive,
                GameTimeTemp, gameTime, TimeAccumulator, 
                FixedTimeStep, CameraSystem, SystemManager, 
                DebugRender, SceneManager
            );
            
            //// UI & MOUSE CURSORS - delegate to UI module
            try
            {
                DrawUI();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("UI DRAW CRASH: " + ex.Message);
            }
            
            EngineGraphicsModule.DrawCursor(CursorTexture, CursorHotspot);
            #endregion
            
            //// DEBUG UI
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
