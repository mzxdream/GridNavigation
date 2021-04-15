using UnityEngine;

public enum GridNavDirOpt { Left = 1, Right = 2, Up = 4, Down = 8 }

public static class GridNavMath
{
    public static float DistanceApproximately(int sx, int sz, int ex, int ez)
    {
        int dx = Mathf.Abs(ex - sx);
        int dz = Mathf.Abs(ez - sz);
        return (dx + dz) + Mathf.Min(dx, dz) * (1.4142f - 2.0f);
    }
}