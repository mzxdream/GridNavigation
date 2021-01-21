using UnityEngine;

public class MathUtils
{
    public static float Distance2D(Vector3 a, Vector3 b)
    {
        return Mathf.Sqrt(SqrDistance2D(a, b));
    }
    public static float SqrDistance2D(Vector3 a, Vector3 b)
    {
        return (b.x - a.x) * (b.x - a.x) + (b.z - a.z) * (b.z - a.z);
    }
    public static bool CompareApproximately(Vector3 a, Vector3 b)
    {
        return SqrDistance2D(a, b) < 1e-06f;
    }
    public static int CompareApproximately(float a, float b)
    {
        return Mathf.Abs(a - b) < 1e-06f ? 0 : (a < b ? -1 : 1);
    }
    public static int Sign(float v)
    {
        return v > 0.0f ? 1 : -1;
    }
}