using UnityEngine;

namespace GridNav
{
    public static class NavUtils
    {
        // 前、后、左、右、左前、右前、左后、右后
        private static readonly int[] neighborDirX = { 0, 0, 0, -1, 1, -1, 1, -1, 1 };
        private static readonly int[] neighborDirZ = { 0, 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly Vector3[] dirVector3 = {
             new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1), new Vector3(-1, 0, 0), new Vector3(1, 0, 0),
             new Vector3(-NavMathUtils.HALF_SQRT2, 0, NavMathUtils.HALF_SQRT2), new Vector3(NavMathUtils.HALF_SQRT2, 0, NavMathUtils.HALF_SQRT2),
             new Vector3(-NavMathUtils.HALF_SQRT2, 0, -NavMathUtils.HALF_SQRT2), new Vector3(NavMathUtils.HALF_SQRT2, 0, -NavMathUtils.HALF_SQRT2)
        };
        private static readonly float[] dirDistance = {
            0, 1.0f, 1.0f, 1.0f, 1.0f, NavMathUtils.SQRT2, NavMathUtils.SQRT2, NavMathUtils.SQRT2, NavMathUtils.SQRT2
        };

        public static int SquareIndex(int x, int z)
        {
            Debug.Assert(x >= 0 && x <= 0xFFFF && z >= 0 && z <= 0x7FFF);
            return x + (z << 16);
        }
        public static void SquareXZ(int index, out int x, out int z)
        {
            Debug.Assert(index >= 0 && index < 0x7FFFFFFF);
            x = index & 0xFFFF;
            z = index >> 16;
        }
        public static Vector3 DirToVector3(NavDirection dir)
        {
            return dirVector3[(int)dir];
        }
        public static void GetNeighborXZ(int x, int z, NavDirection dir, out int nx, out int nz)
        {
            nx = x + neighborDirX[(int)dir];
            nz = z + neighborDirZ[(int)dir];
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
        public static float GetSquareSpeed(NavMap navMap, NavAgent agent, int x, int z)
        {
            var slope = navMap.GetSquareSlope(x, z);
            var squareType = navMap.GetSquareType(x, z);
            Debug.Assert(squareType >= 0 && squareType < agent.moveParam.speedMods.Length);
            return agent.moveParam.speedMods[squareType] / (1.0f + slope * agent.moveParam.slopeMod);
        }
        public static float GetSquareSpeed(NavMap navMap, NavAgent agent, int x, int z, Vector3 moveDir)
        {
            var slope = navMap.GetSquareSlope(x, z);
            var squareType = navMap.GetSquareType(x, z);
            var centerNormal2D = navMap.GetSquareCenterNormal2D(x, z);
            var dirSlopeMod = -NavMathUtils.Dot2D(NavMathUtils.Normalized2D(moveDir), centerNormal2D);
            Debug.Assert(squareType >= 0 && squareType < agent.moveParam.speedMods.Length);
            return agent.moveParam.speedMods[squareType] / (1.0f + Mathf.Max(0.0f, slope * dirSlopeMod) * agent.moveParam.slopeMod);
        }
        public static bool TestMoveSquareCenter(NavMap navMap, NavAgent agent, int x, int z)
        {
            Debug.Assert(x >= 0 && x < navMap.XSize && z >= 0 && z < navMap.ZSize);
            var squareType = navMap.GetSquareType(x, z);
            Debug.Assert(squareType >= 0 && squareType < agent.moveParam.speedMods.Length);

            var speedMod = agent.moveParam.speedMods[squareType];
            if (speedMod <= 0.0f)
            {
                return false;
            }
            var slope = navMap.GetSquareSlope(x, z);
            if (slope > agent.moveParam.maxSlope)
            {
                return false;
            }
            return true;
        }
        public static bool TestMoveSquare(NavMap navMap, NavAgent agent, int x, int z)
        {
            Debug.Assert(x >= 0 && x < navMap.XSize && z >= 0 && z < navMap.ZSize);
            var halfUnitSize = agent.moveParam.unitSize >> 1;
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
        public static NavBlockType TestBlockType(NavAgent collider, NavAgent collidee, bool isExcludeMoving = false)
        {
            if (collider == collidee)
            {
                return NavBlockType.None;
            }
            if (!isExcludeMoving && collidee.isMoving)
            {
                return NavBlockType.Moving;
            }
            if (collidee.param.isPushResistant)
            {
                return NavBlockType.Block;
            }
            if (collidee.moveState != NavMoveState.Idle)
            {
                return NavBlockType.Busy;
            }
            return NavBlockType.Idle;
        }
        public static NavBlockType TestBlockTypesSquareCenter(NavBlockingObjectMap blockingObjectMap, NavAgent agent, int x, int z, bool isExcludeMoving = false)
        {
            if (!blockingObjectMap.GetSquareAgents(x, z, out var agentList))
            {
                return NavBlockType.None;
            }
            var blockTypes = NavBlockType.None;
            foreach (var other in agentList)
            {
                blockTypes |= TestBlockType(agent, other, isExcludeMoving);
                if ((blockTypes & NavBlockType.Block) != 0)
                {
                    break;
                }
            }
            return blockTypes;
        }
        public static NavBlockType TestBlockTypesSquare(NavBlockingObjectMap blockingObjectMap, NavAgent agent, int x, int z, bool isExcludeMoving = false)
        {
            int xmin = x - agent.halfUnitSize;
            int xmax = x + agent.halfUnitSize;
            int zmin = z - agent.halfUnitSize;
            int zmax = z + agent.halfUnitSize;

            var blockTypes = NavBlockType.None;
            for (int tz = zmin; tz <= zmax; tz++)
            {
                for (int tx = xmin; tx <= xmax; tx++)
                {
                    blockTypes |= TestBlockTypesSquareCenter(blockingObjectMap, agent, tx, tz, isExcludeMoving);
                    if ((blockTypes & NavBlockType.Block) != 0)
                    {
                        return blockTypes;
                    }
                }
            }
            return blockTypes;
        }
        public static bool IsSquareBlocked(NavMap navMap, NavBlockingObjectMap blockingObjectMap, NavAgent agent, int x, int z, bool isExcludeMoving = false)
        {
            Debug.Assert(x >= 0 && x < navMap.XSize && z >= 0 && z < navMap.ZSize);
            return !TestMoveSquare(navMap, agent, x, z) || (TestBlockTypesSquare(blockingObjectMap, agent, x, z, isExcludeMoving) & NavBlockType.Block) != 0;
        }
        public static bool IsSquareBlocked(NavMap navMap, NavBlockingObjectMap blockingObjectMap, NavAgent agent, int index, bool isExcludeMoving = false)
        {
            SquareXZ(index, out var x, out var z);
            return IsSquareBlocked(navMap, blockingObjectMap, agent, x, z, isExcludeMoving);
        }
    }
}