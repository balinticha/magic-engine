namespace MagicEngine.Engine.Base;

/// <summary>
/// This file stores the common types used along the engine that do not fit in other folders
/// </summary>
public enum LookupResult
{
    InvalidRequest = 0,
    NotFound = 1,
    Success = 2
}

/// <summary>
/// 2D integer based position pair
/// </summary>
public struct Point2 : IEquatable<Point2>
{
    public int X;
    public int Y;
    
    public Point2(int x, int y)
    {
        X = x;
        Y = y;
    }
    
    public static readonly Point2 Zero = new Point2(0, 0);
    public static readonly Point2 One = new Point2(1, 1);
    public static readonly Point2 Up = new Point2(0, 1);
    public static readonly Point2 Down = new Point2(0, -1);
    public static readonly Point2 Left = new Point2(-1, 0);
    public static readonly Point2 Right = new Point2(1, 0);
    
    public static Point2 operator +(Point2 a, Point2 b) => new Point2(a.X + b.X, a.Y + b.Y);
    public static Point2 operator -(Point2 a, Point2 b) => new Point2(a.X - b.X, a.Y - b.Y);
    public static Point2 operator *(Point2 a, int scalar) => new Point2(a.X * scalar, a.Y * scalar);
    public static Point2 operator *(int scalar, Point2 a) => a * scalar;
    public static Point2 operator -(Point2 a) => new Point2(-a.X, -a.Y);
    
    public bool Equals(Point2 other) => X == other.X && Y == other.Y;

    public override bool Equals(object? obj) => obj is Point2 other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public static bool operator ==(Point2 left, Point2 right) => left.Equals(right);
    public static bool operator !=(Point2 left, Point2 right) => !(left == right);
    
    public override string ToString() => $"({X}, {Y})";
    
    public void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }
    
    public double DistanceTo(Point2 other)
    {
        int dx = X - other.X;
        int dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
    
    public int ManhattanDistanceTo(Point2 other)
    {
        return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
    }
}