using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MagicEngine.Engine.Base;

public class GraphicsManager
{
    public GraphicsDeviceManager Graphics;
    public SpriteBatch SpriteBatch;
    public RenderTarget2D RenderTarget;
    public RenderTarget2D ShadowTarget;
    public RenderTarget2D ScreenTarget; // High Resolution target
    public RenderTarget2D ShadowScreenTarget;


    public ScreenSetup Screen;

    public GraphicsManager(GraphicsDeviceManager graphics, int viewportWidth, int viewportHeight, int padding)
    {
        Graphics = graphics;
        Screen = new ScreenSetup(viewportWidth, viewportHeight, padding);
    }

    public void Initialize()
    {
        Graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        Graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        Graphics.HardwareModeSwitch = false;
        Graphics.IsFullScreen = true;

        Graphics.SynchronizeWithVerticalRetrace = true;
        Graphics.GraphicsProfile = GraphicsProfile.HiDef;
        Graphics.ApplyChanges();
    }

    public void InitializeRenderTargets(GraphicsDevice graphicsDevice)
    {
        RenderTarget = new RenderTarget2D(
            graphicsDevice,
            Screen.VirtualWidth + Screen.Padding * 2,
            Screen.VirtualHeight + Screen.Padding * 2,
            false,
            SurfaceFormat.HalfVector4,
            DepthFormat.None);

        ShadowTarget = new RenderTarget2D(
            graphicsDevice,
            Screen.VirtualWidth + Screen.Padding * 2,
            Screen.VirtualHeight + Screen.Padding * 2,
            false,
            SurfaceFormat.HalfVector4,
            DepthFormat.None);

        ScreenTarget = new RenderTarget2D(graphicsDevice,
            graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height,
            false,
            SurfaceFormat.HalfVector4,
            DepthFormat.None);

        ShadowScreenTarget = new RenderTarget2D(graphicsDevice,
            graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height,
            false,
            SurfaceFormat.HalfVector4,
            DepthFormat.None);
    }
}

public readonly struct ScreenSetup(int viewportWidth, int viewportHeight, int padding)
{
    public readonly int VirtualWidth = viewportWidth;
    public readonly int VirtualHeight = viewportHeight;
    public readonly int Padding = padding;
}