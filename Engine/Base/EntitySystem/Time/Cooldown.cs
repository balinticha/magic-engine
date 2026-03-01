using MagicEngine.Engine.Base.EntitySystem;

namespace MagicEngine.Engine.Base.EntitySystem.Time;

public struct Cooldown
{
    private double _lastActivationTime;
    public float Duration;

    public Cooldown(float duration)
    {
        Duration = duration;
        _lastActivationTime = -duration; // So it's instantly ready
    }

    public bool IsReady(in Timing timing)
    {
        return timing.TotalTime - _lastActivationTime >= Duration;
    }

    public void Reset(in Timing timing)
    {
        _lastActivationTime = timing.TotalTime;
    }

    public float GetRemainingPercent(in Timing timing)
    {
        double elapsed = timing.TotalTime - _lastActivationTime;
        if (elapsed >= Duration) return 0f;
        return (float)(1.0 - (elapsed / Duration));
    }
}

public struct UnscaledCooldown
{
    private double _lastActivationTime;
    public float Duration;

    public UnscaledCooldown(float duration)
    {
        Duration = duration;
        _lastActivationTime = -duration;
    }

    public bool IsReady(in Timing timing)
    {
        return timing.UnscaledTotalTime - _lastActivationTime >= Duration;
    }

    public void Reset(in Timing timing)
    {
        _lastActivationTime = timing.UnscaledTotalTime;
    }

    public float GetRemainingPercent(in Timing timing)
    {
        double elapsed = timing.UnscaledTotalTime - _lastActivationTime;
        if (elapsed >= Duration) return 0f;
        return (float)(1.0 - (elapsed / Duration));
    }
}

public struct RealtimeCooldown
{
    private double _lastActivationTime;
    public float Duration;

    public RealtimeCooldown(float duration)
    {
        Duration = duration;
        _lastActivationTime = -duration;
    }

    public bool IsReady(in Timing timing)
    {
        return timing.RealTotalTime - _lastActivationTime >= Duration;
    }

    public void Reset(in Timing timing)
    {
        _lastActivationTime = timing.RealTotalTime;
    }

    public float GetRemainingPercent(in Timing timing)
    {
        double elapsed = timing.RealTotalTime - _lastActivationTime;
        if (elapsed >= Duration) return 0f;
        return (float)(1.0 - (elapsed / Duration));
    }
}