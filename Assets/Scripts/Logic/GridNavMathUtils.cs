using UnityEngine;

namespace GridNav
{
    public static class NavMathUtils
    {
        public const float EPSILON = 1e-6f;
        public const float SQRT2 = 1.41421356237f;
        public const float HALF_SQRT2 = 0.70710678118f;

        public static float Sign(float f)
        {
            return f >= 0f ? 1f : -1f;
        }
        public static int Square(int a)
        {
            return a * a;
        }
        public static float Square(float a)
        {
            return a * a;
        }
        public static float OctileDistance(int sx, int sz, int ex, int ez)
        {
            const float D = 1.0f;
            const float D2 = NavMathUtils.SQRT2;
            int dx = Mathf.Abs(ex - sx);
            int dz = Mathf.Abs(ez - sz);
            return D * (dx + dz) + (D2 - 2.0f * D) * Mathf.Min(dx, dz);
        }
        public static int SqrDistanceGrid(int sx, int sz, int ex, int ez)
        {
            return (ex - sx) * (ex - sx) + (ez - sz) * (ez - sz);
        }
        public static float SqrDistance2D(Vector3 a, Vector3 b)
        {
            return (b.x - a.x) * (b.x - a.x) + (b.z - a.z) * (b.z - a.z);
        }
        public static float SqrMagnitude2D(Vector3 a)
        {
            return a.x * a.x + a.z * a.z;
        }
        public static float Magnitude2D(Vector3 a)
        {
            return Mathf.Sqrt(a.x * a.x + a.z * a.z);
        }
        public static Vector3 Normalized2D(Vector3 a)
        {
            var mag = (a.x * a.x + a.z * a.z);
            if (mag < EPSILON)
            {
                return Vector3.zero;
            }
            mag = Mathf.Sqrt(mag);
            return new Vector3(a.x / mag, 0.0f, a.z / mag);
        }
        public static float Dot2D(Vector3 a, Vector3 b)
        {
            return a.x * b.x + a.z * b.z;
        }
        public static float Det2D(Vector3 a, Vector3 b) // 如果结果大于0表示b在a的逆时针方向，小于0表示b在a的顺时针方向，等于0表示相等
        {
            return a.x * b.z - a.z * b.x;
        }
        public static float LeftOf2D(Vector3 a, Vector3 b, Vector3 c)
        {
            return Det2D(a - c, b - a);
        }
        public static float Angle2D(Vector3 a, Vector3 b)
        {
            var mag = (a.x * a.x + a.z * a.z) * (b.x * b.x + b.z * b.z);
            if (mag < EPSILON)
            {
                return 0.0f;
            }
            var dot = Mathf.Clamp(Dot2D(a, b) / Mathf.Sqrt(mag), -1.0f, 1.0f);
            return Mathf.Rad2Deg * Mathf.Acos(dot);
        }
        public static float AngleSigned2D(Vector3 from, Vector3 to)
        {
            var angle = Angle2D(from, to);
            return from.x * to.z - from.z * to.x > 0 ? -angle : angle;
        }
        public static Vector3 Rotate2D(Vector3 a, float angle)
        {
            var b = Mathf.Deg2Rad * angle;
            var sinb = Mathf.Sin(b);
            var cosb = Mathf.Cos(b);
            var x = a.x * cosb + a.z * sinb;
            var z = -a.x * sinb + a.z * cosb;
            return new Vector3(x, 0.0f, z).normalized;
        }
        public static Vector3 Rotate2D(Vector3 from, Vector3 to, float maxAngle)
        {
            var angle = Angle2D(from, to);
            if (angle <= maxAngle)
            {
                return to;
            }
            return from.x * to.z - from.z * to.x > 0 ? Rotate2D(from, -maxAngle) : Rotate2D(from, maxAngle);
        }
        public static float SqrPointToSegmentDistance2D(Vector3 p, Vector3 a, Vector3 b)
        {
            var cross = (b.x - a.x) * (p.x - a.x) + (b.z - a.z) * (p.z - a.z);
            if (cross <= EPSILON)
            {
                return (p.x - a.x) * (p.x - a.x) + (p.z - a.z) * (p.z - a.z);
            }
            var d2 = (b.x - a.x) * (b.x - a.x) + (b.z - a.z) * (b.z - a.z);
            if (cross >= d2)
            {
                return (p.x - b.x) * (p.x - b.x) + (p.z - b.z) * (p.z - b.z);
            }
            var r = cross / d2;
            var x = a.x + (b.x - a.x) * r;
            var z = a.z + (b.z - a.z) * r;
            return (p.x - x) * (p.x - x) + (p.z - z) * (p.z - z);
        }
        public static bool IsSegmentCircleIntersection(Vector3 circleCenter, float circleRadius, Vector3 startPos, Vector3 endPos, out float t)
        {
            t = 0.0f;

            var dx = endPos.x - startPos.x;
            var dz = endPos.z - startPos.z;

            var a = dx * dx + dz * dz;
            var b = 2 * (dx * (startPos.x - circleCenter.x) + dz * (startPos.z - circleCenter.z));
            var c = (startPos.x - circleCenter.x) * (startPos.x - circleCenter.x) + (startPos.z - circleCenter.z) * (startPos.z - circleCenter.z) - circleRadius * circleRadius;

            var determinate = b * b - 4 * a * c;
            if (a <= EPSILON || determinate < -EPSILON)
            {
                return false;
            }
            if (determinate < EPSILON && determinate > -EPSILON)
            {
                t = -b / (2 * a);
                return t >= 0.0f && t <= 1.0f;
            }
            float tmp = Mathf.Sqrt(determinate);
            var t1 = (-b + tmp) / (2 * a);
            var t2 = (-b - tmp) / (2 * a);
            if ((t1 < 0.0f && t2 > 1.0f) || (t1 > 1.0f && t2 < 0.0f))
            {
                t = 0.0f;
                return true;
            }
            if (t1 >= 0.0f && t1 <= 1.0f)
            {
                t = t1;
                if (t2 >= 0.0f && t2 <= 1.0f)
                {
                    t = Mathf.Min(t, t2);
                }
                return true;
            }
            t = t2;
            return t >= 0.0f && t <= 1.0f;
        }
        public static bool ClosestHeightPointTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c, out float h)
        {
            h = 0.0f;

            var v0 = c - a;
            var v1 = b - a;
            var v2 = p - a;

            var denom = v0[0] * v1[2] - v0[2] * v1[0];
            if (Mathf.Abs(denom) < EPSILON)
            {
                return false;
            }

            var u = v1[2] * v2[0] - v1[0] * v2[2];
            var v = v0[0] * v2[2] - v0[2] * v2[0];

            if (denom < 0)
            {
                denom = -denom;
                u = -u;
                v = -v;
            }

            if (u >= 0.0f && v >= 0.0f && (u + v) <= denom)
            {
                h = a[1] + (v0[1] * u + v1[1] * v) / denom;
                return true;
            }
            return false;
        }
    }
}