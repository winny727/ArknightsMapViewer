using ArknightsMap;
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

    public static Vector2Int operator -(Vector2Int v)
    {
        return new Vector2Int(-v.x, -v.y);
    }
    public static Vector2Int operator +(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.x + b.x, a.y + b.y);
    }
    public static Vector2Int operator -(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.x - b.x, a.y - b.y);
    }
    public static Vector2Int operator *(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.x * b.x, a.y * b.y);
    }
    public static Vector2Int operator *(int a, Vector2Int b)
    {
        return new Vector2Int(a * b.x, a * b.y);
    }
    public static Vector2Int operator *(Vector2Int a, int b)
    {
        return new Vector2Int(a.x * b, a.y * b);
    }
    public static Vector2Int operator /(Vector2Int a, int b)
    {
        return new Vector2Int(a.x / b, a.y / b);
    }
    public static bool operator ==(Vector2Int lhs, Vector2Int rhs)
    {
        return lhs.x == rhs.x && lhs.y == rhs.y;
    }
    public static bool operator !=(Vector2Int lhs, Vector2Int rhs)
    {
        return !(lhs == rhs);
    }

    public static implicit operator Vector2(Vector2Int v)
    {
        return new Vector2(v.x, v.y);
    }

    #endregion

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

    public static float Dot(Vector2 lhs, Vector2 rhs)
    {
        return lhs.x * rhs.x + lhs.y * rhs.y;
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

    public static implicit operator Offset(Vector2 v)
    {
        return new Offset
        {
            x = v.x,
            y = v.y,
        };
    }

    public static implicit operator Vector2(Offset offset)
    {
        return new Vector2
        {
            x = offset.x,
            y = offset.y,
        };
    }
}

