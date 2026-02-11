using ImGuiNET;
using MagicEngine.Engine.Base.Debug.UI;
using MagicEngine.Engine.ECS.Core.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MagicEngine.Engine.Base.DebugModule;

public interface IDebugWindow
{
    public bool IsOpen { get; set; }
    public Keys Hotkey { get; protected set; }
    
    public void Draw();
}

public class EngineDebugModule : IEngineDebugModule
{
    public bool DebugEnabled { get; set; }
    
    private ImGuiRenderer _imGuiRenderer;
    private List<IDebugWindow> _windows;

    private int _keyCooldown = 0;
    private GraphicsDeviceManager _graphics;

    public EngineDebugModule(List<IDebugWindow> windows, Game game, GraphicsDeviceManager graphics)
    {
        DebugEnabled = false;
        _windows = windows;
        _graphics = graphics;
        
        _imGuiRenderer = new ImGuiRenderer(game);
        _imGuiRenderer.RebuildFontAtlas();
    }
    
    public void AddWindow(IDebugWindow window) { _windows.Add(window); }
    public void RemoveWindow(IDebugWindow window) { _windows.Remove(window); }

    public void HandleInput(KeyboardState input)
    {
        if (_keyCooldown > 0)
        {
            _keyCooldown--;
            return;
        }

        foreach (var window in _windows)
        {
            if (input.IsKeyDown(window.Hotkey))
            {
                window.IsOpen = !window.IsOpen;
            }
        }
        
    }

    public void Draw(GameTime gameTime)
    {
        if (!DebugEnabled)
        {
            return;
        }

        _graphics.GraphicsDevice.BlendState = BlendState.Opaque;
        _graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        _graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        _graphics.GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

        _imGuiRenderer.BeforeLayout(gameTime);
                
        ImGui.GetForegroundDrawList().AddCircleFilled(ImGui.GetIO().MousePos, 5f, 0xFF0000FF);
        
        foreach (var window in _windows)
        {
            window.Draw();
        }
        
        _imGuiRenderer.AfterLayout();
    }

}