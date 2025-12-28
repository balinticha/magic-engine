using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MagicThing.Engine.Base;

public class GraphicsManager
{
    public GraphicsDeviceManager Graphics;
    public SpriteBatch SpriteBatch;
    public RenderTarget2D RenderTarget;
    public RenderTarget2D ShadowTarget;
    public RenderTarget2D ScreenTarget;  // High Resolution target
    public RenderTarget2D ShadowScreenTarget;

    public ScreenSetup Screen;

    public GraphicsManager(GraphicsDeviceManager graphics, int viewportWidth, int viewportHeight, int padding)
    {
        Graphics = graphics;
        Screen = new ScreenSetup(viewportWidth, viewportHeight, padding);
    }
}

public readonly struct ScreenSetup(int viewportWidth, int viewportHeight, int padding)
{
    public readonly int VirtualWidth = viewportWidth;
    public readonly int VirtualHeight = viewportHeight;
    public readonly int Padding = padding;
}