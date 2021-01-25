using UnityEngine;

public class MathUtils
{
    public const int NUM_HEADINGS = 4096;
    public const int MAX_HEADING = 32768;
    public static Vector3[] headingToVectorTable;
    public static void Init()
    {
        headingToVectorTable = new Vector3[NUM_HEADINGS];
        for (int a = 0; a < NUM_HEADINGS; ++a)
        {
            float ang = (a - (NUM_HEADINGS / 2)) * Mathf.PI * 2.0f / NUM_HEADINGS;
            headingToVectorTable[a].x = Mathf.Sin(ang);
            headingToVectorTable[a].z = Mathf.Cos(ang);
        }
    }
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
    public static int GetHeadingFromVector(Vector3 vec3)
    {
        float s = MAX_HEADING / Mathf.PI;
        float h = 0.0f;
        if (vec3.z != 0.0f)
        {
            float sz = vec3.z * 2.0f - 1.0f;
            float d = vec3.x / (vec3.z + sz * 0.000001f);
            if (Mathf.Abs(d) > 1.0f)
            {
                if (d >= 0.0f)
                {
                    h = d * Mathf.PI * 0.5f - d / (d * d + 0.28f);
                }
                else
                {
                    h = -d * Mathf.PI * 0.5f - d / (d * d + 0.28f);
                }
            }
            else
            {
                h = d / (1.0f + 0.28f * d * d);
            }
            if (vec3.z < 0.0f)
            {
                if (vec3.x > 0)
                {
                    h += Mathf.PI;
                }
                else
                {
                    h -= Mathf.PI;
                }
            }
        }
        else
        {
            h = vec3.x > 0.0f ? Mathf.PI * 0.5f : Mathf.PI * -0.5f;
        }
        int ih = (int)h;
        ih += (ih == -MAX_HEADING ? 1 : 0);
        ih %= MAX_HEADING;
        return ih;
    }
    public static Vector3 GetVectorFromHeading(int heading)
    {
        int div = (MAX_HEADING / NUM_HEADINGS) * 2;
        int idx = heading / div + NUM_HEADINGS / 2;
        return headingToVectorTable[idx];
    }
}