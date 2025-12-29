using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic; // Added for List<T>
using System.Linq; // Keep for convenience, but we won't use it in the hot path
using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using System.Numerics;

namespace MagicEngine.Engine.Base.Debug.UI;

public class DiagnosticsPanel
{
    private float[] _frameTimeHistory = new float[600];
    private int _historyIndex = 0;

    // --- OPTIMIZATION 1: Pre-allocate buffers to avoid "new" in Update/Draw loop ---
    private float[] _sortedFrameTimes = new float[600];
    
    // Cache the list of key-value pairs to avoid ToList() allocations
    private List<KeyValuePair<string, ProfilerHistory>> _cachedSystemList = new();
    
    // Create a static comparer to avoid delegate allocation during Sort
    private static readonly Comparison<KeyValuePair<string, ProfilerHistory>> _systemComparer = 
        (a, b) => b.Value.GetAverage().CompareTo(a.Value.GetAverage());

    public void Draw(ref bool isOpen, GraphicsDevice graphicsDevice, double fps, double frameTime, SystemProfiler profiler)
    {
        if (!isOpen) return;

        ImGui.SetNextWindowSize(new Vector2(600, 600), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Diagnostics", ref isOpen))
        {
            if (ImGui.CollapsingHeader("Graphics Metrics", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var metrics = graphicsDevice.Metrics;
                DrawMetricNoAlloc("Draw Calls: ", metrics.DrawCount);
                DrawMetricNoAlloc("Primitives: ", metrics.PrimitiveCount);
                DrawMetricNoAlloc("Texture Switches: ", metrics.TextureCount);
                DrawMetricNoAlloc("Clear Count: ", metrics.ClearCount);
                DrawMetricNoAlloc("Target Switches: ", metrics.TargetCount);
            }
            
            if (ImGui.CollapsingHeader("Performance", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawMetricNoAlloc("FPS: ", fps, new StandardFormat('F', 1));
                DrawMetricNoAlloc("Frame Time: ", frameTime, new StandardFormat('F', 2));

                _frameTimeHistory[_historyIndex] = (float)frameTime;
                _historyIndex = (_historyIndex + 1) % _frameTimeHistory.Length;
                
                Array.Copy(_frameTimeHistory, _sortedFrameTimes, _frameTimeHistory.Length);
                Array.Sort(_sortedFrameTimes);
                
                int index99 = (int)(_frameTimeHistory.Length * 0.99f);
                int index90 = (int)(_frameTimeHistory.Length * 0.90f);
                
                index99 = Math.Clamp(index99, 0, _frameTimeHistory.Length - 1);
                index90 = Math.Clamp(index90, 0, _frameTimeHistory.Length - 1);

                float p99 = _sortedFrameTimes[index99];
                float p90 = _sortedFrameTimes[index90];

                ImGui.Text($"1%% High (99th): {p99:F3} ms");
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(1, 1, 0, 1), $"10%% High (90th): {p90:F3} ms");

                Vector2 graphSize = new Vector2(0, 80);
                float graphMaxY = 33f;
                
                ImGui.PlotLines("##FrameTimeGraph", ref _frameTimeHistory[0], _frameTimeHistory.Length, _historyIndex, $"{frameTime:F3} ms", 0, graphMaxY, graphSize);
                
                if (ImGui.IsItemVisible())
                {
                   Vector2 pMin = ImGui.GetItemRectMin();
                   Vector2 pMax = ImGui.GetItemRectMax();
                   var drawList = ImGui.GetWindowDrawList();
                   float height = pMax.Y - pMin.Y;
                   float y99 = pMax.Y - (p99 / graphMaxY * height);
                   float y90 = pMax.Y - (p90 / graphMaxY * height);
                   y99 = Math.Clamp(y99, pMin.Y, pMax.Y);
                   y90 = Math.Clamp(y90, pMin.Y, pMax.Y);
                   drawList.AddLine(new Vector2(pMin.X, y99), new Vector2(pMax.X, y99), ImGui.GetColorU32(new Vector4(1, 0, 0, 0.7f)));
                   drawList.AddLine(new Vector2(pMin.X, y90), new Vector2(pMax.X, y90), ImGui.GetColorU32(new Vector4(1, 1, 0, 0.7f)));
                }
            }
            
            if (ImGui.CollapsingHeader("System Profiler", ImGuiTreeNodeFlags.DefaultOpen))
            {
                _cachedSystemList.Clear();

                foreach(var kvp in profiler.SystemTimes)
                {
                    _cachedSystemList.Add(kvp);
                }
                
                _cachedSystemList.Sort(_systemComparer);

                ImGui.Columns(4, "system_columns");
                ImGui.Separator();
                ImGui.Text("System"); ImGui.NextColumn();
                ImGui.Text("Avg (ms)"); ImGui.NextColumn();
                ImGui.Text("10% High"); ImGui.NextColumn();
                ImGui.Text("1% High"); ImGui.NextColumn();
                ImGui.Separator();
                
                foreach (var system in _cachedSystemList)
                {
                    var history = system.Value;
                    double avg = history.GetAverage();
                    
                    double p90 = history.GetPercentile(0.90f);
                    double p99 = history.GetPercentile(0.99f);

                    ImGui.TextUnformatted(system.Key); 
                    ImGui.NextColumn();
                    
                    DrawMetricNoAlloc("", avg, new StandardFormat('F', 3), GetColor(avg)); 
                    ImGui.NextColumn();

                    DrawMetricNoAlloc("", p90, new StandardFormat('F', 3), GetColor(p90)); 
                    ImGui.NextColumn();

                    DrawMetricNoAlloc("", p99, new StandardFormat('F', 3), GetColor(p99)); 
                    ImGui.NextColumn();
                }
                ImGui.Columns(1);
            }
        }
        ImGui.End();
    }
    
    private static Vector4 GetColor(double time)
    {
        if (time > 5.0) return new Vector4(1, 0, 0, 1);
        if (time > 2.0) return new Vector4(1, 1, 0, 1);
        return new Vector4(0, 1, 0, 1);
    }
    
    // 1. A reusable scratch buffer for text formatting
    private byte[] _textBuffer = new byte[256];

    // 2. Helper to draw "Label: Value" without string allocations
    private unsafe void DrawMetricNoAlloc(string prefix, double value, StandardFormat format, Vector4? color = null)
    {
        // A. Copy the prefix string (e.g., "FPS: ") into the buffer
        // Assuming ASCII/UTF8 compatible prefixes. 
        int offset = 0;
        foreach (char c in prefix)
        {
            if (offset >= _textBuffer.Length - 10) break; // Safety check
            _textBuffer[offset++] = (byte)c;
        }

        // B. Format the number directly into the buffer (No string object created!)
        if (Utf8Formatter.TryFormat(value, _textBuffer.AsSpan(offset), out int bytesWritten, format))
        {
            offset += bytesWritten;
        }

        // C. Null-terminate the string for C++
        _textBuffer[offset] = 0;

        // D. Pass the pointer directly to ImGui Native
        fixed (byte* ptr = _textBuffer)
        {
            if (color.HasValue)
            {
                ImGuiNative.igTextColored(color.Value, ptr);
            }
            else
            {
                ImGuiNative.igTextUnformatted(ptr, null);
            }
        }
    }

    // 3. Overload for Integer metrics (Draw Calls, etc.)
    private unsafe void DrawMetricNoAlloc(string prefix, long value, Vector4? color = null)
    {
        int offset = 0;
        foreach (char c in prefix) _textBuffer[offset++] = (byte)c;

        if (Utf8Formatter.TryFormat(value, _textBuffer.AsSpan(offset), out int bytesWritten, default))
        {
            offset += bytesWritten;
        }

        _textBuffer[offset] = 0;
        fixed (byte* ptr = _textBuffer)
        {
            if (color.HasValue) ImGuiNative.igTextColored(color.Value, ptr);
            else ImGuiNative.igTextUnformatted(ptr, null);
        }
    }
}