using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using ImGuiNET;
using MagicThing.Engine.Base.Debug.Commands;

namespace MagicThing.Engine.Base.Debug.UI;

public unsafe class DebugConsoleWindow
{
    private readonly CommandManager _commandManager;
    private readonly ConsoleInterceptor _consoleInterceptor;
    private byte[] _inputBuffer = new byte[100];
    private bool _scrollToBottom;
    
    private readonly List<string> _commandHistory = new();
    private int _historyIndex = 0;
    private const int MaxLogLines = 1000;
    
    private byte[] _callbackBuffer = new byte[512];

    public DebugConsoleWindow(CommandManager commandManager, ConsoleInterceptor consoleInterceptor)
    {
        _commandManager = commandManager;
        _consoleInterceptor = consoleInterceptor;
        _scrollToBottom = true;
    }

    public unsafe void Draw(ref bool isOpen)
    {
        if (!isOpen) return;

        ImGui.SetNextWindowSize(new Vector2(1400, 600), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Debug Console", ref isOpen))
        {
            try
            {
                // Log Area
                ImGui.BeginChild("ScrollingRegion", new Vector2(0, -ImGui.GetFrameHeightWithSpacing()),
                    ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);

                var logMessages = _consoleInterceptor.LogMessages;
                var start = Math.Max(0, logMessages.Count - MaxLogLines);
                for (var i = start; i < logMessages.Count; i++)
                {
                    ImGui.TextUnformatted(logMessages[i]);
                }

                if (_scrollToBottom)
                {
                    ImGui.SetScrollHereY(1.0f);
                    _scrollToBottom = false;
                }

                ImGui.EndChild();

                ImGui.Separator();

                // Input Text Box
                bool reclaimFocus = false;
                var inputTextFlags = ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CallbackHistory;
                
                if (ImGui.InputText("Input", _inputBuffer, (uint)_inputBuffer.Length, inputTextFlags, ConsoleInputCallback))
                {
                    HandleInput();
                    reclaimFocus = true;
                }
                
                ImGui.SameLine();
                if (ImGui.Button("Submit"))
                {
                    HandleInput();
                    reclaimFocus = true;
                }

                if (reclaimFocus)
                {
                    ImGui.SetKeyboardFocusHere(-1); // Auto-focus the input box
                }
            }
            finally
            {
                ImGui.End();
            }
        }
        else
        {
            ImGui.End();
        }
    }

    private unsafe int ConsoleInputCallback(ImGuiInputTextCallbackData* data)
    {
        // Wrap the raw pointer in the ImGui.NET helper object
        var dataPtr = new ImGuiInputTextCallbackDataPtr(data);

        // Now, use the wrapper object (dataPtr) to access flags and methods
        if (dataPtr.EventFlag != ImGuiInputTextFlags.CallbackHistory)
        {
            return 0;
        }

        int prevHistoryIndex = _historyIndex;
        if (dataPtr.EventKey == ImGuiKey.UpArrow)
        {
            if (_historyIndex > 0) _historyIndex--;
        }
        else if (dataPtr.EventKey == ImGuiKey.DownArrow)
        {
            if (_historyIndex < _commandHistory.Count) _historyIndex++;
        }

        if (prevHistoryIndex != _historyIndex)
        {
            string historyText = (_historyIndex < _commandHistory.Count)
                ? _commandHistory[_historyIndex]
                : "";

            // Call the helper methods on the wrapper object
            dataPtr.DeleteChars(0, dataPtr.BufTextLen);
            dataPtr.InsertChars(0, historyText);
        }

        return 0;
    }
    
    private void HandleInput()
    {
        string input = Encoding.UTF8.GetString(_inputBuffer).TrimEnd('\0'); // Get string from buffer, remove null terminators
        Array.Clear(_inputBuffer, 0, _inputBuffer.Length); // Clear the buffer for the next input

        if (!string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine($"> {input}"); 
            
            if (_commandHistory.LastOrDefault() != input)
            {
                _commandHistory.Add(input);
            }
            _historyIndex = _commandHistory.Count;
            
            
            string result = _commandManager.ExecuteCommand(input);
            if(!string.IsNullOrEmpty(result))
            {
                Console.WriteLine(result);
            }
            
            _scrollToBottom = true;
        }
    }
}