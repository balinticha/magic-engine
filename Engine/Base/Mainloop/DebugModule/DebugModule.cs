using System;
using System.Collections.Generic;
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
    public Keys Hotkey { get; }
    
    // Managed windows are not opened or closed by hotkeys, but rather are managed by other windows.
    // When they are closed, they can be removed from the module's managed list.
    public bool IsManaged { get; }
    
    public void Draw(GameTime gameTime);
}

// Ensure the module interface is defined properly if not elsewhere, but looks like it was not defined in this file. Let's define it so we can use IEngineDebugModule
public interface IEngineDebugModule
{
    bool DebugEnabled { get; set; }
    void AddWindow(IDebugWindow window);
    void RemoveWindow(IDebugWindow window);
    void HandleInput(KeyboardState input);
    void Draw(GameTime gameTime);
    void ShowCrash(Exception e);
    bool WantsCaptureKeyboard { get; }
}

public class EngineDebugModule : IEngineDebugModule
{
    public bool DebugEnabled { get; set; }
    public bool WantsCaptureKeyboard => ImGui.GetIO().WantCaptureKeyboard;
    
    private ImGuiRenderer _imGuiRenderer;
    private List<IDebugWindow> _windows;

    private int _keyCooldown = 0;
    private GraphicsDeviceManager _graphics;

    // Optional event or direct method to show crash inspector
    public Action<Exception> OnCrash; 

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

    public void ShowCrash(Exception e)
    {
        OnCrash?.Invoke(e);
    }

    public void HandleInput(KeyboardState input)
    {
        if (_keyCooldown > 0)
        {
            _keyCooldown--;
            return;
        }

        bool stateChanged = false;
        foreach (var window in _windows)
        {
            if (window.Hotkey != Keys.None && input.IsKeyDown(window.Hotkey))
            {
                window.IsOpen = !window.IsOpen;
                stateChanged = true;
            }
        }

        if (stateChanged)
        {
            _keyCooldown = 15; // Set some cooldown
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
        
        // Clean up managed windows that are closed
        _windows.RemoveAll(w => w.IsManaged && !w.IsOpen);

        // Convert to array to allow windows to add other windows during their Draw call
        var windowsArray = _windows.ToArray();
        foreach (var window in windowsArray)
        {
            if (window.IsOpen)
            {
                window.Draw(gameTime);
            }
        }
        
        _imGuiRenderer.AfterLayout();
    }
}