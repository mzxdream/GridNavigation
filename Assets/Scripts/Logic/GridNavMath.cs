using UnityEngine;

public enum GridNavDirection { None = 0, Left = 1, Right = 2, Up = 3, Down = 4, LeftUp = 5, RightUp = 6, LeftDown = 7, RightDown = 8 }
public enum GridNavDirectionOpt { None = 0, Left = 1, Right = 2, Up = 4, Down = 8 }

public static class GridNavMath
{
    private static readonly GridNavDirection[] optToDirs = {
        GridNavDirection.None, GridNavDirection.Left,     GridNavDirection.Right,     GridNavDirection.None,
        GridNavDirection.Up,   GridNavDirection.LeftUp,   GridNavDirection.RightUp,   GridNavDirection.None,
        GridNavDirection.Down, GridNavDirection.LeftDown, GridNavDirection.RightDown, GridNavDirection.None,
        GridNavDirection.None, GridNavDirection.None,     GridNavDirection.None,      GridNavDirection.None
    };
    private static readonly GridNavDirectionOpt[] dirToOpts = {
        GridNavDirectionOpt.None, GridNavDirectionOpt.Left, GridNavDirectionOpt.Right, GridNavDirectionOpt.Up, GridNavDirectionOpt.Down,
        GridNavDirectionOpt.Left | GridNavDirectionOpt.Up, GridNavDirectionOpt.Right | GridNavDirectionOpt.Up,
        GridNavDirectionOpt.Left | GridNavDirectionOpt.Down, GridNavDirectionOpt.Right | GridNavDirectionOpt.Down
    };

    public static float DistanceApproximately(int sx, int sz, int ex, int ez)
    {
        int dx = Mathf.Abs(ex - sx);
        int dz = Mathf.Abs(ez - sz);
        return (dx + dz) + Mathf.Min(dx, dz) * (1.4142f - 2.0f);
    }
    public static GridNavDirection ToDirection(GridNavDirectionOpt opt)
    {
        return optToDirs[(int)opt];
    }
    public static GridNavDirectionOpt ToDirectionOpt(GridNavDirection dir)
    {
        return dirToOpts[(int)dir];
    }
    public static GridNavDirection CombineDirection(GridNavDirection dir1, GridNavDirection dir2)
    {
        return optToDirs[(int)(dirToOpts[(int)dir1] | dirToOpts[(int)dir2])];
    }
    public static float SqrDistance2D(Vector3 a, Vector3 b)
    {
        return (b.x - a.x) * (b.x - a.x) + (b.z - a.z) * (b.z - a.z);
    }
    public static Vector3 Normalized2D(Vector3 a)
    {
        var mag = (a.x * a.x + a.z * a.z);
        if (mag < 0.000001f)
        {
            return Vector3.zero;
        }
        mag = Mathf.Sqrt(mag);
        return new Vector3(a.x / mag, 0.0f, a.z / mag);
    }
}