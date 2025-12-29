using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MagicEngine.Engine.Base.Debug;

public class SystemProfiler
{
    public Dictionary<string, ProfilerHistory> SystemTimes = new();

    // 1. We remove the global _stopwatch field because it breaks nesting.
    // Instead, we use raw timestamps.

    // 2. This is the new method you call in your code
    public ProfileScope Profile(string name)
    {
        // Creates a lightweight struct on the STACK (no GC memory)
        return new ProfileScope(this, name);
    }

    // Helper method called by the struct when it finishes
    public void Record(string systemName, double elapsedMs)
    {
        if (!SystemTimes.TryGetValue(systemName, out var history))
        {
            history = new ProfilerHistory();
            SystemTimes[systemName] = history;
        }
        history.Add(elapsedMs);
    }

    // 3. The Magic Struct (Must be 'readonly struct' for best performance)
    public readonly struct ProfileScope : IDisposable
    {
        private readonly SystemProfiler _profiler;
        private readonly string _name;
        private readonly long _startTicks;

        public ProfileScope(SystemProfiler profiler, string name)
        {
            _profiler = profiler;
            _name = name;
            _startTicks = Stopwatch.GetTimestamp();
        }

        public void Dispose()
        {
            long endTicks = Stopwatch.GetTimestamp();
            // Convert ticks to milliseconds
            double ms = (endTicks - _startTicks) * 1000.0 / Stopwatch.Frequency;
            _profiler.Record(_name, ms);
        }
    }
    
    public void Remove(string systemName)
    {
        SystemTimes.Remove(systemName);
    }
}

// CHANGED: struct -> class
public class ProfilerHistory
{
    private const int Capacity = 600;
    
    private double[] _samples = new double[Capacity]; // Pre-allocate immediately
    private double[] _buffer = new double[Capacity];  // Pre-allocate a scratchpad for sorting
    
    private int _count;
    private int _index;

    public void Add(double value)
    {
        _samples[_index] = value;
        _index = (_index + 1) % Capacity;
        if (_count < Capacity) _count++;
    }

    public double GetAverage()
    {
        if (_count == 0) return 0;
        
        double sum = 0;
        for (int i = 0; i < _count; i++)
        {
            sum += _samples[i];
        }
        return sum / _count;
    }

    public double GetPercentile(float percentile)
    {
        if (_count == 0) return 0;

        // FIXED: Reuse the pre-allocated _buffer instead of "new double[]"
        Array.Copy(_samples, _buffer, _count);
        
        // Sort only the valid part of the buffer
        Array.Sort(_buffer, 0, _count);

        int index = (int)Math.Ceiling(percentile * _count) - 1;
        index = Math.Max(0, Math.Min(index, _count - 1));
        
        return _buffer[index];
    }
}