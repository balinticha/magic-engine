using System;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace MagicEngine.Engine.Base.Debug.UI;

public class CrashInspectorPanel
{
    public bool IsActive { get; private set; }

    private Exception _crashException;
    private readonly Game _gameInstance; 

    // State for the flashing animation
    private float _flashTimer;
    private int _flashCount;
    private bool _isInitialFlashActive;
    
    private const int TotalFlashes = 5; // The number of times to flash
    private const float FlashSequenceDuration = 1f; // The total time for all flashes
    // A single flash has two states (e.g., on/off), so we calculate the interval for each state change.
    private const float FlashStateInterval = FlashSequenceDuration / (TotalFlashes * 2);

    public CrashInspectorPanel(Game gameInstance)
    {
        _gameInstance = gameInstance;
        IsActive = false;
    }

    /// <summary>
    /// Activates the crash inspector, displaying it and halting the game.
    /// </summary>
    public void Activate(Exception e)
    {
        IsActive = true;
        _crashException = e;
        
        // Reset animation state
        _flashTimer = 0f;
        _flashCount = 0;
        _isInitialFlashActive = true;
    }

    /// <summary>
    /// Draws the crash inspector window using ImGui.
    /// </summary>
    public void Draw(GameTime gameTime)
    {
        // Don't draw if not active
        if (!IsActive || _crashException == null)
        {
            return;
        }

        // --- Animation Logic ---
        // Only run the animation logic if the initial flash sequence is active.
        if (_isInitialFlashActive)
        {
            _flashTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_flashTimer >= FlashStateInterval)
            {
                _flashCount++;
                _flashTimer -= FlashStateInterval;

                // Stop flashing after the desired number of state changes (5 flashes = 10 changes).
                if (_flashCount >= TotalFlashes * 2)
                {
                    _isInitialFlashActive = false;
                }
            }
        }

        // --- Styling ---
        bool isBrightState = !_isInitialFlashActive || (_flashCount % 2 == 0);
        
        if (isBrightState)
        {
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(1.0f, 0.0f, 0.0f, 0.8f));
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.8f, 0.0f, 0.0f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.6f, 0.0f, 0.0f, 0.8f));
        }
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

        // --- Window Layout ---
        Vector2 displaySize = ImGui.GetIO().DisplaySize;
        Vector2 windowSize = new Vector2(1400, 700);
        ImGui.SetNextWindowPos(new Vector2((displaySize.X - windowSize.X) * 0.5f, (displaySize.Y - windowSize.Y) * 0.5f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(windowSize, ImGuiCond.FirstUseEver);

        // --- Window Content ---
        ImGui.Begin("Crash Inspector", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove);

        ImGui.TextColored(new Vector4(1.0f, 0.2f, 0.2f, 1.0f), "CRITICAL APPLICATION FAILURE DETECTED!");
        ImGui.Separator();

        ImGui.TextWrapped("The game has encountered an unhandled exception and normal execution is permanently terminated.");
        ImGui.TextWrapped("You can use the debug tools to inspect the game state at the time of the crash, and exit the game when ready.");
        ImGui.TextWrapped("The crash log is displayed under this text.");
        ImGui.Separator();

        ImGui.BeginChild("StackTrace", new Vector2(0, -ImGui.GetFrameHeightWithSpacing()), ImGuiChildFlags.Borders);
        ImGui.TextUnformatted(_crashException.ToString());
        ImGui.EndChild();

        if (ImGui.Button("Copy Error to Clipboard"))
        {
            ImGui.SetClipboardText(_crashException.ToString());
        }

        ImGui.SameLine();
        if (ImGui.Button("Exit Game"))
        {
            _gameInstance.Exit();
        }

        ImGui.End();

        // --- Cleanup ---
        ImGui.PopStyleColor(3);
    }
}