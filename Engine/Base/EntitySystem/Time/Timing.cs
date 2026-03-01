namespace MagicEngine.Engine.Base.EntitySystem.Time;

public readonly struct Timing(float realDeltaTime, double realTotalTime, float deltaTime, double totalTime, float unscaledDeltaTime, double unscaledTotalTime, float alpha)
{
    public readonly float RealDeltaTime = realDeltaTime;
    public readonly double RealTotalTime = realTotalTime;
    
    public readonly float DeltaTime = deltaTime;
    public readonly double TotalTime = totalTime;
    
    public readonly float UnscaledDeltaTime = unscaledDeltaTime;
    public readonly double UnscaledTotalTime = unscaledTotalTime;
    
    public readonly float Alpha = alpha;
}