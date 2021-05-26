using UnityEngine;

namespace GridNav
{
    public static class NavMathUtils
    {
        public static readonly float EPSILON = 1e-4f;
        public static readonly float SQRT2 = Mathf.Sqrt(2);
        public static readonly float HALF_SQRT2 = SQRT2 * 0.5f;
        private static readonly NavDirection[] optToDirs = {
            NavDirection.None, NavDirection.Forward, NavDirection.Back, NavDirection.None,
            NavDirection.Left, NavDirection.LeftForward, NavDirection.LeftBack, NavDirection.None,
            NavDirection.Right, NavDirection.RightForward, NavDirection.RightBack, NavDirection.None,
            NavDirection.None, NavDirection.None, NavDirection.None, NavDirection.None
        };
        private static readonly NavDirectionOpt[] dirToOpts = {
            NavDirectionOpt.None, NavDirectionOpt.Forward, NavDirectionOpt.Back, NavDirectionOpt.Left, NavDirectionOpt.Right,
            NavDirectionOpt.Left | NavDirectionOpt.Forward, NavDirectionOpt.Right | NavDirectionOpt.Forward,
            NavDirectionOpt.Left | NavDirectionOpt.Back, NavDirectionOpt.Right | NavDirectionOpt.Back
        };
        private static readonly int[] neighborDirX = { 0, 0, 0, -1, 1, -1, 1, -1, 1 };
        private static readonly int[] neighborDirZ = { 0, 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly Vector3[] dirVector3 = { new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1), new Vector3(-1, 0, 0), new Vector3(1, 0, 0), new Vector3(-HALF_SQRT2, 0, HALF_SQRT2), new Vector3(HALF_SQRT2, 0, HALF_SQRT2), new Vector3(-HALF_SQRT2, 0, -HALF_SQRT2), new Vector3(-HALF_SQRT2, 0, -HALF_SQRT2) };
        private static readonly float[] dirCost = { 0, 1.0f, 1.0f, 1.0f, 1.0f, SQRT2, SQRT2, SQRT2, SQRT2 };

        public static NavDirection CombineDirection(NavDirection dir1, NavDirection dir2)
        {
            return optToDirs[(int)(dirToOpts[(int)dir1] | dirToOpts[(int)dir2])];
        }
        public static Vector3 DirToVector3(NavDirection dir)
        {
            return dirVector3[(int)dir];
        }
        public static float DirCost(NavDirection dir)
        {
            return dirCost[(int)dir];
        }
        public static void GetNeighborXZ(int x, int z, NavDirection dir, out int nx, out int nz)
        {
            nx = x + neighborDirX[(int)dir];
            nz = z + neighborDirZ[(int)dir];
        }
        public static float DistanceApproximately(int sx, int sz, int ex, int ez)
        {
            int dx = Mathf.Abs(ex - sx);
            int dz = Mathf.Abs(ez - sz);
            return (dx + dz) + Mathf.Min(dx, dz) * (1.4142f - 2.0f);
        }
        public static float DistanceApproximately(int startIndex, int endIndex)
        {
            NavUtils.GetSquareXZ(startIndex, out var sx, out var sz);
            NavUtils.GetSquareXZ(endIndex, out var ex, out var ez);
            return DistanceApproximately(sx, sz, ex, ez);
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
            if (mag < 0.000001f)
            {
                return Vector3.zero;
            }
            mag = Mathf.Sqrt(mag);
            return new Vector3(a.x / mag, 0.0f, a.z / mag);
        }
        public static float LeftOf2D(Vector3 a, Vector3 b, Vector3 c)
        {
            return Det2D(a - c, b - a);
        }
        public static float Angle2D(Vector3 a, Vector3 b)
        {
            var x1 = a.x;
            var z1 = a.z;
            var d = Mathf.Sqrt(x1 * x1 + z1 * z1);
            if (d > Mathf.Epsilon)
            {
                x1 /= d;
                z1 /= d;
            }
            else
            {
                x1 = 0;
                z1 = 0;
            }
            var x2 = b.x;
            var z2 = b.z;
            d = Mathf.Sqrt(x2 * x2 + z2 * z2);
            if (d > Mathf.Epsilon)
            {
                x2 /= d;
                z2 /= d;
            }
            else
            {
                x2 = 0;
                z2 = 0;
            }
            d = x1 * x2 + z1 * z2;
            return Mathf.Rad2Deg * Mathf.Acos(Mathf.Clamp(d, -1, 1));
        }
        public static float Angle2DSigned(Vector3 from, Vector3 to)
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
            return new Vector3(x, a.y, z);
        }
        public static Vector3 Rotate2D(Vector3 from, Vector3 to, float maxAngle)
        {
            var angle = Angle2D(from, to);
            if (angle <= maxAngle)
            {
                return to;
            }
            return from.x * to.z - from.z * to.x > 0 ? Rotate2D(from, -angle) : Rotate2D(from, angle);
        }
        public static float Dot2D(Vector3 a, Vector3 b)
        {
            return a.x * b.x + a.z * b.z;
        }
        public static float Det2D(Vector3 a, Vector3 b)
        {
            return a.x * b.z - a.z * b.x;
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