using System;
using System.Collections.Generic;

public struct Vector2Int
{
    public int x;
    public int y;
    public float sqrMagnitude => x * x + y * y;

    public Vector2Int(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
    public override string ToString()
    {
        return $"({x},{y})";
    }

    #region operator

    public static Vector2Int operator +(Vector2Int vec1, Vector2Int vec2)
    {
        return new Vector2Int(vec1.x + vec2.x, vec1.y + vec2.y);
    }

    public static Vector2Int operator -(Vector2Int vec1, Vector2Int vec2)
    {
        return new Vector2Int(vec1.x - vec2.x, vec1.y - vec2.y);
    }
    public static bool operator ==(Vector2Int vec1, Vector2Int vec2)
    {
        return vec1.x == vec2.x && vec1.y == vec2.y;
    }
    public static bool operator !=(Vector2Int vec1, Vector2Int vec2)
    {
        return !(vec1 == vec2);
    }

    public static implicit operator Vector2(Vector2Int v)
    {
        return new Vector2(v.x, v.y);
    }

    public static implicit operator ArknightsMap.Position(Vector2Int v)
    {
        return new ArknightsMap.Position
        {
            col = v.x,
            row = v.y,
        };
    }

    public static implicit operator Vector2Int(ArknightsMap.Position position)
    {
        return new Vector2Int
        {
            x = position.col,
            y = position.row,
        };
    }

    #endregion
}

public struct Vector2
{
    public float x;
    public float y;
    public float sqrMagnitude => x * x + y * y;
    public float magnitude => (float)Math.Sqrt(x * x + y * y);
    public Vector2 normalized => new Vector2(x, y) / magnitude;

    public Vector2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
    public override string ToString()
    {
        return $"({x},{y})";
    }

    #region operator

    public static Vector2 operator +(Vector2 a, Vector2 b)
    {
        return new Vector2(a.x + b.x, a.y + b.y);
    }
    public static Vector2 operator -(Vector2 a, Vector2 b)
    {
        return new Vector2(a.x - b.x, a.y - b.y);
    }
    public static Vector2 operator *(Vector2 a, Vector2 b)
    {
        return new Vector2(a.x * b.x, a.y * b.y);
    }
    public static Vector2 operator /(Vector2 a, Vector2 b)
    {
        return new Vector2(a.x / b.x, a.y / b.y);
    }
    public static Vector2 operator -(Vector2 a)
    {
        return new Vector2(0f - a.x, 0f - a.y);
    }
    public static Vector2 operator *(Vector2 a, float d)
    {
        return new Vector2(a.x * d, a.y * d);
    }
    public static Vector2 operator *(float d, Vector2 a)
    {
        return new Vector2(a.x * d, a.y * d);
    }
    public static Vector2 operator /(Vector2 a, float d)
    {
        return new Vector2(a.x / d, a.y / d);
    }
    public static bool operator ==(Vector2 lhs, Vector2 rhs)
    {
        float num = lhs.x - rhs.x;
        float num2 = lhs.y - rhs.y;
        return num * num + num2 * num2 < 9.99999944E-11f;
    }
    public static bool operator !=(Vector2 lhs, Vector2 rhs)
    {
        return !(lhs == rhs);
    }

    #endregion
}

