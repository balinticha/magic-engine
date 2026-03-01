using MagicEngine.Engine.Base.Debug;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MagicEngine.Engine.Base.Mainloop.CoreModules;

public interface IEngineGraphicsModule
{
    public GraphicsManager GraphicsManager { get; }
    public LogManager LogManager { get; }
    public GameWindow Window { get; }

    public void LoadContent();
    public void DrawCursor(Texture2D cursorTexture, Vector2 cursorHotspot);

    // todo actually make this generic and decoupled,
    // probably make this accept a generic EngineGraphicsModuleRuntimeData struct or smth
    // for now, we'll leave this commented out
    // public void Draw(smth);
}