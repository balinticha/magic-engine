using MagicEngine.Engine.Base.EntitySystem;

namespace MagicEngine.Engine.Base.EntitySystem.Time;

public class TimeManager
{
    public const float FixedTimeStep = 1f / 60f;
    private double _timeAccumulator = 0f;

    public float RealDeltaTime { get; private set; }
    public double RealTotalTime { get; private set; }

    public float DeltaTime { get; private set; }
    public double TotalTime { get; private set; }

    public float UnscaledDeltaTime { get; private set; }
    public double UnscaledTotalTime { get; private set; }

    public void Update(float realDeltaTime, bool isPaused, float gameSpeed)
    {
        RealDeltaTime = realDeltaTime;
        RealTotalTime += realDeltaTime;

        if (isPaused)
        {
            UnscaledDeltaTime = 0f;
            DeltaTime = 0f;
        }
        else
        {
            UnscaledDeltaTime = realDeltaTime;
            UnscaledTotalTime += UnscaledDeltaTime;

            DeltaTime = realDeltaTime * gameSpeed;
            TotalTime += DeltaTime;

            _timeAccumulator += DeltaTime;
        }
    }

    public bool ShouldRunFixedUpdate()
    {
        return _timeAccumulator >= FixedTimeStep;
    }

    public void ConsumeFixedUpdate()
    {
        _timeAccumulator -= FixedTimeStep;
    }

    public Timing GetCurrentTiming()
    {
        return new Timing(
            RealDeltaTime, RealTotalTime,
            DeltaTime, TotalTime,
            UnscaledDeltaTime, UnscaledTotalTime,
            0f);
    }

    public Timing GetFixedTiming()
    {
        return new Timing(
            FixedTimeStep, RealTotalTime,
            FixedTimeStep, TotalTime,
            FixedTimeStep, UnscaledTotalTime,
            1.0f);
    }

    public Timing GetInterpolatedTiming()
    {
        float alpha = (float)(_timeAccumulator / FixedTimeStep);
        return new Timing(
            RealDeltaTime, RealTotalTime,
            DeltaTime, TotalTime,
            UnscaledDeltaTime, UnscaledTotalTime,
            alpha);
    }
}