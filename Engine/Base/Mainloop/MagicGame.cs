using DefaultEcs;
using MagicEngine.Engine.Base.Debug;
using MagicEngine.Engine.Base.Debug.Commands;
using MagicEngine.Engine.Base.Debug.UI;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.Base.EntitySystem.Time;
using MagicEngine.Engine.Base.Events;
using MagicEngine.Engine.Base.Mainloop.CoreModules;
using MagicEngine.Engine.Base.Mainloop.DebugModule;
using MagicEngine.Engine.Base.PrototypeComponentSystem;
using MagicEngine.Engine.Base.Scene;
using MagicEngine.Engine.Base.Shaders.PostProcessing;
using MagicEngine.Engine.ECS.Core.Camera;
using MagicEngine.Engine.ECS.Core.Input;
using MagicEngine.Engine.ECS.Core.Physics.Behavior;
using MagicEngine.Engine.ECS.Core.Positioning;
using MagicEngine.Engine.ECS.Core.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;

namespace MagicEngine.Engine.Base.Mainloop;

public abstract class MagicGame : Game
{
    #region New modules
    private GraphicsManager _gp;
    public EngineGraphicsModule EngineGraphicsModule;
    public EngineDebugModule EngineDebugModule;
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
    protected bool _hasCrashed = false;
    protected CommandManager _commandManager;
    protected ConsoleInterceptor _consoleInterceptor;
    #endregion
    
    #region State
    protected bool DebugRender;
    protected int DebugRenderCooldown;
    protected Random _random;
    protected bool _consoleActive = false;

    protected TimeManager TimeManager;
    
    
    // Profiling
    private double _fps;
    private double _frameTime;
    private const float Smoothing = 0.95f;
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
        TimeManager = new TimeManager();
        
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
        
        LogManager.Log("Startup: Debug view content load", LogLevel.VerboseExtra);
        SceneManager.GetScene().AttachedSystems.DebugView.LoadContent(GraphicsDevice, Content);
        
        DebugRender = false;
        DebugRenderCooldown = 10;

        LogManager.Log("Startup: ImGuiRender", LogLevel.VerboseExtra);
        EngineDebugModule = new EngineDebugModule(new List<IDebugWindow>(), this, _gp.Graphics);
        
        EngineDebugModule.OnCrash = (e) => {
            _hasCrashed = true;
            EngineDebugModule.DebugEnabled = true;
            var crashPanel = new CrashInspectorPanel(this);
            crashPanel.Activate(e);
            EngineDebugModule.AddWindow(crashPanel);
        };
        
        // Add default panels
        EngineDebugModule.AddWindow(new DebugConsoleWindow(_commandManager, _consoleInterceptor));
        EngineDebugModule.AddWindow(new PostProcessDebugOverlay(EngineGraphicsModule.PostProcessingManager));
        EngineDebugModule.AddWindow(new DiagnosticsPanel(
            () => _fps, 
            () => _frameTime, 
            GraphicsDevice, 
            () => SystemManager.Profiler
        ));
        
        var sceneGraphPanel = SceneManager.GetScene().AttachedSystems.SceneGraphPanel;
        EngineDebugModule.AddWindow(sceneGraphPanel);
        sceneGraphPanel.OnInspectEntity += entity => {
            EngineDebugModule.AddWindow(new ComponentViewerPanel(entity, entity.GetHashCode()));
        };
        
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
            if (_hasCrashed)
            {
                base.Update(gameTime);
                return;
            }
            
            UpdateUI(gameTime);
            
            // ========= Debug stuff start
            EngineDebugModule.HandleInput(Keyboard.GetState());
            
            if (Keyboard.GetState().IsKeyDown(Keys.F1) && DebugRenderCooldown <= 0)
            {
                DebugRender  = !DebugRender;
                EngineDebugModule.DebugEnabled = !EngineDebugModule.DebugEnabled;
                DebugRenderCooldown = 20;
            }
            DebugRenderCooldown--;
            // ========= Debug stuff end
            
            var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // FPS & FrameTime Calculation
            if (deltaTime > 0.000001f)
            {
                double currentFps = 1.0 / deltaTime;
                _fps = (_fps * Smoothing) + (currentFps * (1.0 - Smoothing));
            }
            _frameTime = deltaTime * 1000.0;
            
            var sessionManager = SystemManager.GetSystem<SessionManager>();
            TimeManager.Update(deltaTime, sessionManager.IsPaused, sessionManager.GameSpeed);
            
            var preLoopTiming = TimeManager.GetCurrentTiming();
            
            SystemManager.RunFrameStart(preLoopTiming);
            SystemManager.RunTransientUpdate(preLoopTiming);
            
            if (sessionManager.IsPaused)
            {
                SystemManager.RunPausedUpdate(preLoopTiming);
                RunPausedFrameUpdate(preLoopTiming);
            }
            else
            {
                RunPreFixedUpdateContent(preLoopTiming);
                
                while (TimeManager.ShouldRunFixedUpdate())
                {
                    var fixedTiming = TimeManager.GetFixedTiming();
                    SystemManager.RunFixedUpdatePrePhysics(fixedTiming);
                
                    // Engine housekeeping
                    SystemManager.GetSystem<ProcessPositionRequestSystem>().ManualUpdate();
                
                    // Physics bridge and the system itself ---
                    using (SystemManager.Profiler.Profile("Physics: BodyCreation"))
                    {
                        SceneManager.GetScene().AttachedSystems.BodyCreationSystem.Update(TimeManager.FixedTimeStep);
                    }
                    using (SystemManager.Profiler.Profile("Physics: PreSync"))
                    {
                        SceneManager.GetScene().AttachedSystems.PrePhysicsSyncSystem.Update(TimeManager.FixedTimeStep);
                    }
                    using (SystemManager.Profiler.Profile("Physics: Simulation"))
                    {
                        SceneManager.GetScene().PhysicsWorld.Step(TimeManager.FixedTimeStep);
                        SceneManager.GetScene().PhysicsWorld.ClearForces();
                    }
                    using (SystemManager.Profiler.Profile("Physics: PostSync"))
                    {
                        SceneManager.GetScene().AttachedSystems.PostPhysicsSyncSystem.Update(TimeManager.FixedTimeStep);
                    }
                    using (SystemManager.Profiler.Profile("Physics: BodyDeletion"))
                    {
                        SceneManager.GetScene().AttachedSystems.BodyDeletionSystem.Update(TimeManager.FixedTimeStep);
                    }
                    using (SystemManager.Profiler.Profile("Physics: WeldManager"))
                    {
                        SystemManager.GetSystem<WeldManagerSystem>().Update(fixedTiming);
                    }
                    // ----------------------------------------
                
                    SystemManager.RunFixedUpdatePostPhysics(fixedTiming);
                    
                    RunFixedUpdateContent(fixedTiming);
                    TimeManager.ConsumeFixedUpdate();
                }
            
                var postLoopTiming = TimeManager.GetInterpolatedTiming();
            
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
            if (!_hasCrashed)
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
                _hasCrashed,
                TimeManager.GetInterpolatedTiming(), CameraSystem, SystemManager, 
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
            var inputManager = SystemManager.GetSystem<InputManagerSystem>();
            inputManager.IsInputDisabled = _hasCrashed || (EngineDebugModule.WantsCaptureKeyboard && EngineDebugModule.DebugEnabled);

            EngineDebugModule.Draw(gameTime);
            #endregion
        }
        catch (Exception e)
        {
            if (!_hasCrashed)
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
        EngineDebugModule.ShowCrash(e);
        LogManager.Debug("Entering crash inspector.");
    }
}
