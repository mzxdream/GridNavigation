using System;
using UnityEngine;

namespace GridNav
{
    public static class NavUtils
    {
        // None, Forward, Back, Left, Right, LeftForward, RightForward, LeftBack, RightBack
        private static readonly int[,] dirNeighbor = { { 0, 0 }, { 0, 1 }, { 0, -1 }, { -1, 0 }, { 1, 0 }, { -1, 1 }, { 1, 1 }, { -1, -1 }, { 1, -1 } };
        private static readonly Vector3[] dirVector3 = {
             new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1), new Vector3(-1, 0, 0), new Vector3(1, 0, 0),
             new Vector3(-NavMathUtils.HALF_SQRT2, 0, NavMathUtils.HALF_SQRT2), new Vector3(NavMathUtils.HALF_SQRT2, 0, NavMathUtils.HALF_SQRT2),
             new Vector3(-NavMathUtils.HALF_SQRT2, 0, -NavMathUtils.HALF_SQRT2), new Vector3(NavMathUtils.HALF_SQRT2, 0, -NavMathUtils.HALF_SQRT2)
        };
        private static readonly float[] dirDistance = {
            0, 1.0f, 1.0f, 1.0f, 1.0f, NavMathUtils.SQRT2, NavMathUtils.SQRT2, NavMathUtils.SQRT2, NavMathUtils.SQRT2
        };

        public static float DegreesToSlope(float degrees)
        {
            return 1.0f - Mathf.Cos(degrees * Mathf.Deg2Rad);
        }
        public static Vector3 DirToVector3(NavDirection dir)
        {
            return dirVector3[(int)dir];
        }
        public static void GetNeighborXZ(int x, int z, NavDirection dir, out int nx, out int nz)
        {
            nx = x + dirNeighbor[(int)dir, 0];
            nz = z + dirNeighbor[(int)dir, 1];
        }
        public static float DirDistanceApproximately(NavDirection dir)
        {
            return dirDistance[(int)dir];
        }
        public static float DistanceApproximately(int sx, int sz, int ex, int ez)
        {
            int dx = Mathf.Abs(ex - sx);
            int dz = Mathf.Abs(ez - sz);
            return (dx + dz) + Mathf.Min(dx, dz) * (1.4142f - 2.0f);
        }
        public static float CalcMaxInteriorRadius(int unitSize, float squareSize)
        {
            Debug.Assert(unitSize > 0 && squareSize > 0.0f);

            return (unitSize - 1) * squareSize * 0.5f - NavMathUtils.EPSILON;
        }
        public static float CalcMinExteriorRadius(int unitSize, float squareSize)
        {
            Debug.Assert(unitSize > 0 && squareSize > 0.0f);

            return (unitSize - 1) * squareSize * NavMathUtils.HALF_SQRT2 - NavMathUtils.EPSILON;
        }
        public static Vector2Int CalcMapPos(NavMap navMap, int unitSize, Vector3 pos)
        {
            Debug.Assert(navMap != null && unitSize > 0 && unitSize < navMap.XSize && unitSize < navMap.ZSize);

            navMap.GetSquareXZ(pos.x + navMap.SquareSize * 0.5f, pos.z + navMap.SquareSize * 0.5f, out var x, out var z);
            Vector2Int mapPos = new Vector2Int();
            mapPos.x = Mathf.Clamp(x - (unitSize >> 1), 0, navMap.XSize - unitSize);
            mapPos.y = Mathf.Clamp(z - (unitSize >> 1), 0, navMap.ZSize - unitSize);
            return mapPos;
        }
        public static void ForeachNearestSquare(int x, int z, int radius, Func<int, int, bool> cb)
        {
            Debug.Assert(cb != null);

            for (int k = 1; k < radius; k++)
            {
                int xmin = x - k, xmax = x + k, zmin = z - k, zmax = z + k;
                if (!cb(x, zmax) || !cb(x, zmin) || !cb(xmin, z) || !cb(xmax, z)) // forward, back, left, right
                {
                    return;
                }
                for (int t = 1; t < k; t++)
                {
                    if (!cb(xmin, z + t) || !cb(xmin, z - t) || !cb(xmax, z + t) || !cb(xmax, z - t) // left[forward], left[back], right[forward], right[back]
                        || !cb(x - t, zmax) || !cb(x + t, zmax) || !cb(x - t, zmin) || !cb(x + t, zmin)) // [left]forward, [right]forward, [left]back, [right]back
                    {
                        return;
                    }
                }
                if (!cb(xmin, zmax) || !cb(xmax, zmax) || !cb(xmin, zmin) || !cb(xmax, zmin)) // left forward, right forward, left back, right back
                {
                    return;
                }
            }
        }
        public static float GetSquareSpeed(NavMap navMap, NavAgent agent, int x, int z)
        {
            var slope = navMap.GetSquareSlope(x, z);
            var squareType = navMap.GetSquareType(x, z);
            var speedMod = agent.moveDef.GetSpeedMod(squareType);
            var slopeMod = agent.moveDef.GetSlopeMod();
            return speedMod / (1.0f + slope * slopeMod);
        }
        public static float GetSquareSpeed(NavMap navMap, NavAgent agent, int x, int z, NavDirection dir)
        {
            var slope = navMap.GetSquareSlope(x, z);
            var squareType = navMap.GetSquareType(x, z);
            var centerNormal2D = navMap.GetSquareCenterNormal2D(x, z);
            var moveDir = DirToVector3(dir);
            var dirSlopeMod = -NavMathUtils.Dot2D(moveDir, centerNormal2D);
            var speedMod = agent.moveDef.GetSpeedMod(squareType);
            var slopeMod = agent.moveDef.GetSlopeMod();
            return speedMod / (1.0f + Mathf.Max(0.0f, slope * dirSlopeMod) * slopeMod);
        }
        public static bool TestMoveSquareCenter(NavMap navMap, NavAgent agent, int x, int z)
        {
            Debug.Assert(x >= 0 && x < navMap.XSize && z >= 0 && z < navMap.ZSize);

            var squareType = navMap.GetSquareType(x, z);
            var speedMod = agent.moveDef.GetSpeedMod(squareType);
            if (speedMod <= 0.0f)
            {
                return false;
            }
            var slope = navMap.GetSquareSlope(x, z);
            var maxSlope = agent.moveDef.GetMaxSlope();
            if (slope > maxSlope)
            {
                return false;
            }
            return true;
        }
        public static bool TestMoveSquare(NavMap navMap, NavAgent agent, int x, int z)
        {
            Debug.Assert(x >= 0 && x < navMap.XSize && z >= 0 && z < navMap.ZSize);

            var halfUnitSize = (agent.moveDef.GetUnitSize() - 1) >> 1;
            int xmin = x - halfUnitSize;
            int xmax = x + halfUnitSize;
            int zmin = z - halfUnitSize;
            int zmax = z + halfUnitSize;
            if (xmin < 0 || xmax >= navMap.XSize || zmin < 0 || zmax >= navMap.ZSize)
            {
                return false;
            }
            for (int tz = zmin; tz <= zmax; tz++)
            {
                for (int tx = xmin; tx <= xmax; tx++)
                {
                    if (!TestMoveSquareCenter(navMap, agent, tx, tz))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public static bool IsPushResistant(NavAgent collider, NavAgent collidee)
        {
            if (collider == collidee)
            {
                return false;
            }
            return collidee.param.isPushResistant;
        }
        public static float CalcPriorityRatio(NavAgent collider, NavAgent collidee)
        {
            var pushCollider = IsPushResistant(collidee, collider);
            var pushCollidee = IsPushResistant(collider, collidee);
            if (pushCollider)
            {
                return pushCollidee ? 0.5f : 0.0f;
            }
            if (pushCollidee)
            {
                return pushCollider ? 0.5f : 1.0f;
            }
            if (collider.isMoving)
            {
                return collidee.isMoving ? 0.5f : 0.0f;
            }
            if (collidee.isMoving)
            {
                return collider.isMoving ? 0.5f : 1.0f;
            }
            return collider.id > collidee.id ? 1.0f : 0.0f;
        }
        public static NavBlockType TestBlockType(NavAgent collider, NavAgent collidee)
        {
            Debug.Assert(collider != null && collidee != null);

            if (collider == collidee)
            {
                return NavBlockType.None;
            }
            if (collidee.isMoving)
            {
                return NavBlockType.Moving;
            }
            if (IsPushResistant(collider, collidee))
            {
                return NavBlockType.Blocked;
            }
            if (collidee.moveState != NavMoveState.Idle)
            {
                return NavBlockType.Busy;
            }
            return NavBlockType.Idle;
        }
        public static NavBlockType TestBlockTypesSquareCenter(NavBlockingObjectMap blockingObjectMap, NavAgent agent, int x, int z)
        {
            Debug.Assert(blockingObjectMap != null && agent != null);

            var blockTypes = NavBlockType.None;
            foreach (var other in blockingObjectMap.GetSquareAgents(x, z))
            {
                blockTypes |= TestBlockType(agent, other);
                if ((blockTypes & NavBlockType.Blocked) != 0)
                {
                    break;
                }
            }
            return blockTypes;
        }
        public static NavBlockType TestBlockTypesSquare(NavBlockingObjectMap blockingObjectMap, NavAgent agent, int x, int z)
        {
            Debug.Assert(blockingObjectMap != null && agent != null);

            var halfUnitSize = (agent.moveDef.GetUnitSize() - 1) >> 1;
            int xmin = x - halfUnitSize;
            int xmax = x + halfUnitSize;
            int zmin = z - halfUnitSize;
            int zmax = z + halfUnitSize;

            var blockTypes = NavBlockType.None;
            for (int tz = zmin; tz <= zmax; tz += 2)
            {
                for (int tx = xmin; tx <= xmax; tx += 2)
                {
                    blockTypes |= TestBlockTypesSquareCenter(blockingObjectMap, agent, tx, tz);
                    if ((blockTypes & NavBlockType.Blocked) != 0)
                    {
                        return blockTypes;
                    }
                }
            }
            return blockTypes;
        }
        public static bool IsNoneBlockTypeSquare(NavBlockingObjectMap blockingObjectMap, NavAgent agent, int x, int z)
        {
            Debug.Assert(blockingObjectMap != null && agent != null);

            var halfUnitSize = (agent.moveDef.GetUnitSize() - 1) >> 1;
            int xmin = x - halfUnitSize;
            int xmax = x + halfUnitSize;
            int zmin = z - halfUnitSize;
            int zmax = z + halfUnitSize;

            for (int tz = zmin; tz <= zmax; tz += 2)
            {
                for (int tx = xmin; tx <= xmax; tx += 2)
                {
                    foreach (var other in blockingObjectMap.GetSquareAgents(tx, tz))
                    {
                        if (other != agent)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}