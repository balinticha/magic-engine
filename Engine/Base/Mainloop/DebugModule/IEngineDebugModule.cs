using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace MagicEngine.Engine.Base.DebugModule;

public interface IEngineDebugModule
{
    public bool DebugEnabled { get; set; }

    public void HandleInput(KeyboardState input);
    public void Draw(GameTime gameTime);
}