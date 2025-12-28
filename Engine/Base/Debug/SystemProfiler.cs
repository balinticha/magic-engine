using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MagicEngine.Engine.Base.Debug;

public class SystemProfiler
{
    public Dictionary<string, ProfilerHistory> SystemTimes = new();
    private Stopwatch _stopwatch = new();

    public void Profile(string systemName, Action action)
    {
        _stopwatch.Restart();
        action();
        _stopwatch.Stop();
        
        // Store result
        if (!SystemTimes.TryGetValue(systemName, out var history))
        {
            history = new ProfilerHistory();
            SystemTimes[systemName] = history;
        }

        // We can't modify the struct directly from the dictionary value, so we need to put it back
        history.Add(_stopwatch.Elapsed.TotalMilliseconds);
        SystemTimes[systemName] = history;
    }

    public void Remove(string systemName)
    {
        SystemTimes.Remove(systemName);
    }
}

public struct ProfilerHistory
{
    private const int Capacity = 600;
    private double[] _samples;
    private int _count;
    private int _index;

    public void Add(double value)
    {
        if (_samples == null) _samples = new double[Capacity];

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
        if (_count == 0 || _samples == null) return 0;

        int sortedCount = _count;
        double[] sorted = new double[sortedCount];
        Array.Copy(_samples, sorted, sortedCount);
        Array.Sort(sorted);

        int index = (int)Math.Ceiling(percentile * sortedCount) - 1;
        index = Math.Max(0, Math.Min(index, sortedCount - 1));
        
        return sorted[index];
    }
}
