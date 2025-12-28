namespace MagicEngine.Engine.Base.EntitySystem;

public readonly struct Timing(float deltaTime, float alpha, double gameTime)
{
    public readonly float DeltaTime = deltaTime;
    public readonly float Alpha = alpha;
    public readonly double GameTime = gameTime;
}