using UnityEngine;

namespace GridNav
{
    public static class MathUtils
    {
        private enum DirectionOpt { None = 0, Left = 1, Right = 2, Up = 4, Down = 8 }
        private static readonly Direction[] optToDirs = {
            Direction.None, Direction.Left,     Direction.Right,     Direction.None,
            Direction.Up,   Direction.LeftUp,   Direction.RightUp,   Direction.None,
            Direction.Down, Direction.LeftDown, Direction.RightDown, Direction.None,
            Direction.None, Direction.None,     Direction.None,      Direction.None
        };
        private static readonly DirectionOpt[] dirToOpts = {
            DirectionOpt.None, DirectionOpt.Left, DirectionOpt.Right, DirectionOpt.Up, DirectionOpt.Down,
            DirectionOpt.Left | DirectionOpt.Up, DirectionOpt.Right | DirectionOpt.Up,
            DirectionOpt.Left | DirectionOpt.Down, DirectionOpt.Right | DirectionOpt.Down
        };
        private static readonly int[] dirX = { 0, -1, 1, 0, 0, -1, 1, -1, 1 };
        private static readonly int[] dirZ = { 0, 0, 0, 1, -1, 1, 1, -1, -1 };
        private static readonly float[] dirCost = { 0, 1.0f, 1.0f, 1.0f, 1.0f, 1.4142f, 1.4142f, 1.4142f, 1.4142f };

        public static void SquareXZ(int index, out int x, out int z)
        {
            x = index & 0xFFFF;
            z = index >> 16;
        }
        public static int SquareIndex(int x, int z)
        {
            return x + (z << 16);
        }
        public static Direction CombineDirection(Direction dir1, Direction dir2)
        {
            return optToDirs[(int)(dirToOpts[(int)dir1] | dirToOpts[(int)dir2])];
        }
        public static float DistanceApproximately(int sx, int sz, int ex, int ez)
        {
            int dx = Mathf.Abs(ex - sx);
            int dz = Mathf.Abs(ez - sz);
            return (dx + dz) + Mathf.Min(dx, dz) * (1.4142f - 2.0f);
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
    }
}