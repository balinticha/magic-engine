using System;
using System.Linq;
using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using System.Numerics;

namespace MagicThing.Engine.Base.Debug.UI;

public class DiagnosticsPanel
{
    private float[] _frameTimeHistory = new float[600];
    private int _historyIndex = 0;

    public void Draw(ref bool isOpen, GraphicsDevice graphicsDevice, double fps, double frameTime, SystemProfiler profiler)
    {
        if (!isOpen) return;

        ImGui.SetNextWindowSize(new Vector2(600, 600), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Diagnostics", ref isOpen))
        {
            // --- Graphics Metrics ---
            if (ImGui.CollapsingHeader("Graphics Metrics", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var metrics = graphicsDevice.Metrics;
                ImGui.Text($"Draw Calls: {metrics.DrawCount}");
                ImGui.Text($"Primitives: {metrics.PrimitiveCount}");
                ImGui.Text($"Texture Switches: {metrics.TextureCount}");
                ImGui.Text($"Clear Count: {metrics.ClearCount}");
                ImGui.Text($"Target Switches: {metrics.TargetCount}");
            }

            // --- Performance ---
            if (ImGui.CollapsingHeader("Performance", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Text($"FPS: {fps:F1}");
                ImGui.Text($"Frame Time: {frameTime:F2} ms");

                // Update history
                _frameTimeHistory[_historyIndex] = (float)frameTime;
                _historyIndex = (_historyIndex + 1) % _frameTimeHistory.Length;

                // Calculate percentiles (local calculation since we need float array for PlotLines)
                float[] sortedFrameTimes = new float[_frameTimeHistory.Length];
                Array.Copy(_frameTimeHistory, sortedFrameTimes, _frameTimeHistory.Length);
                Array.Sort(sortedFrameTimes);
                
                // 99th percentile (1% High) and 90th percentile (10% High)
                // Filter out 0s if the buffer isn't full yet to avoid skewing? 
                // Actually the buffer is 0-initialized, which is fine, 0s will just be at the bottom.
                // We want the highest values, so we look at the end of the sorted array.
                
                int index99 = (int)(_frameTimeHistory.Length * 0.99f);
                int index90 = (int)(_frameTimeHistory.Length * 0.90f);
                
                index99 = Math.Clamp(index99, 0, _frameTimeHistory.Length - 1);
                index90 = Math.Clamp(index90, 0, _frameTimeHistory.Length - 1);

                float p99 = sortedFrameTimes[index99];
                float p90 = sortedFrameTimes[index90];

                ImGui.Text($"1%% High (99th): {p99:F3} ms");
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(1, 1, 0, 1), $"10%% High (90th): {p90:F3} ms");

                // Plot Lines
                Vector2 graphSize = new Vector2(0, 80);
                float graphMaxY = 33f; // 33ms ~ 30fps
                
                ImGui.PlotLines("##FrameTimeGraph", ref _frameTimeHistory[0], _frameTimeHistory.Length, _historyIndex, $"{frameTime:F3} ms", 0, graphMaxY, graphSize);
                
                // Draw horizontal lines for percentiles
                if (ImGui.IsItemVisible())
                {
                    Vector2 pMin = ImGui.GetItemRectMin();
                    Vector2 pMax = ImGui.GetItemRectMax();
                    var drawList = ImGui.GetWindowDrawList();

                    // Calculate Y positions (0 is at pMax.Y, graphMaxY is at pMin.Y)
                    // value / max * height
                    // pixelY = pMax.Y - (value / max * height)
                    
                    float height = pMax.Y - pMin.Y;
                    
                    float y99 = pMax.Y - (p99 / graphMaxY * height);
                    float y90 = pMax.Y - (p90 / graphMaxY * height);

                    // Clamp to bounds
                    y99 = Math.Clamp(y99, pMin.Y, pMax.Y);
                    y90 = Math.Clamp(y90, pMin.Y, pMax.Y);

                    drawList.AddLine(new Vector2(pMin.X, y99), new Vector2(pMax.X, y99), ImGui.GetColorU32(new Vector4(1, 0, 0, 0.7f))); // Red for 99th
                    drawList.AddLine(new Vector2(pMin.X, y90), new Vector2(pMax.X, y90), ImGui.GetColorU32(new Vector4(1, 1, 0, 0.7f))); // Yellow for 90th
                }
            }

            // --- Systems Profiler ---
            if (ImGui.CollapsingHeader("System Profiler", ImGuiTreeNodeFlags.DefaultOpen))
            {
                // Sort by average time descending
                var sortedSystems = profiler.SystemTimes.OrderByDescending(kv => kv.Value.GetAverage()).ToList();

                ImGui.Columns(4, "system_columns"); // Increased columns
                ImGui.Separator();
                ImGui.Text("System"); ImGui.NextColumn();
                ImGui.Text("Avg (ms)"); ImGui.NextColumn();
                ImGui.Text("10% High"); ImGui.NextColumn();
                ImGui.Text("1% High"); ImGui.NextColumn();
                ImGui.Separator();

                // Helper local function for coloring
                Vector4 GetColor(double time)
                {
                    if (time > 5.0) return new Vector4(1, 0, 0, 1); // Red
                    if (time > 2.0) return new Vector4(1, 1, 0, 1); // Yellow
                    return new Vector4(0, 1, 0, 1); // Green
                }

                foreach (var system in sortedSystems)
                {
                    var history = system.Value;
                    double avg = history.GetAverage();
                    double p90 = history.GetPercentile(0.90f);
                    double p99 = history.GetPercentile(0.99f);

                    ImGui.Text(system.Key); ImGui.NextColumn();
                    
                    ImGui.TextColored(GetColor(avg), $"{avg:F3}"); ImGui.NextColumn();
                    ImGui.TextColored(GetColor(p90), $"{p90:F3}"); ImGui.NextColumn();
                    ImGui.TextColored(GetColor(p99), $"{p99:F3}"); ImGui.NextColumn();
                }
                ImGui.Columns(1);
            }
        }
        ImGui.End();
    }
}
