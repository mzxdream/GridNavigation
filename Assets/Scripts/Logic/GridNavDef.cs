using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridNav
{
    public enum NavDirection { None = 0, Forward = 1, Back = 2, Left = 3, Right = 4, LeftForward = 5, RightForward = 6, LeftBack = 7, RightBack = 8 }
    public enum NavMoveState { Idle = 0, Requesting = 1, WaitForPath = 2, InProgress = 3 }
    public enum NavBlockType { None = 0, Idle = 1, Busy = 2, Moving = 4, Blocked = 8 };
    public enum NavSpeedModMultType { Idle = 0, Busy = 1, Moving = 2, Blocked = 3, NumTypes = 4 };

    public class NavDef
    {
        public const int MaxAreaTypes = 32;
    }

    public class NavMoveDef
    {
        // speed = speedMod * speedModMult / (1.0f + slope * slopeMod)
        private int unitSize = 4; // >=4的偶数
        private float maxSlope = 0.5f; // 最大爬坡角度(0.0f - 1.0f) => (0 - 90) cos曲线(1-cos60*)
        private float slopeMod = 0.0f; // 爬坡消耗,值越大，消耗越大 
        private float[] speedMods = new float[NavDef.MaxAreaTypes]; // <=0表示不能行走,值越大速度越快
        private float[] speedModMults = new float[(int)NavSpeedModMultType.NumTypes]; // 对应NavSpeedModMultType 值越小，寻路消耗越大

        public NavMoveDef()
        {
            for (int i = 0; i < speedMods.Length; i++)
            {
                speedMods[i] = 1.0f;
            }
            speedModMults[(int)NavSpeedModMultType.Idle] = 0.35f;
            speedModMults[(int)NavSpeedModMultType.Busy] = 0.10f;
            speedModMults[(int)NavSpeedModMultType.Moving] = 0.65f;
            speedModMults[(int)NavSpeedModMultType.Blocked] = 0.01f;
        }
        public void SetUnitSize(int unitSize)
        {
            Debug.Assert(unitSize >= 4 && (unitSize & 1) == 0);

            this.unitSize = unitSize;
        }
        public int GetUnitSize()
        {
            return unitSize;
        }
        public void SetMaxSlope(float maxSlope)
        {
            Debug.Assert(maxSlope >= 0.0f && maxSlope <= 1.0f);

            this.maxSlope = maxSlope;
        }
        public float GetMaxSlope()
        {
            return maxSlope;
        }
        public void SetSlopeMod(float slopeMod)
        {
            this.slopeMod = slopeMod;
        }
        public float GetSlopeMod()
        {
            return slopeMod;
        }
        public void SetSpeedMod(int areaType, float speedMod)
        {
            Debug.Assert(areaType >= 0 && areaType < speedMods.Length);

            speedMods[areaType] = speedMod;
        }
        public float GetSpeedMod(int areaType)
        {
            Debug.Assert(areaType >= 0 && areaType < speedMods.Length);

            return speedMods[areaType];
        }
        public void SetSpeedModMult(NavSpeedModMultType type, float speedModMult)
        {
            Debug.Assert((int)type >= 0 && (int)type < speedModMults.Length);

            speedModMults[(int)type] = speedModMult;
        }
        public float GetSpeedModMult(NavSpeedModMultType type)
        {
            Debug.Assert((int)type >= 0 && (int)type < speedModMults.Length);

            return speedModMults[(int)type];
        }
    }

    public struct NavAgentParam
    {
        public int moveType;
        public int teamID;
        public float mass;
        public float maxSpeed;
        public bool isPushResistant;
    }

    public class NavAgent
    {
        public int id;
        public NavAgentParam param;
        public NavMoveDef moveDef;
        public Vector3 pos;
        public float radius;
        public Vector2Int mapPos;
        public NavMoveState moveState;
        public Vector3 lastPos;
        public Vector3 goalPos;
        public float goalRadius;
        public int topologyOptTime;
        public List<Vector3> path;
        public Vector3 velocity;
        public Vector3 prefVelocity;
        public Vector3 newVelocity;
        public bool isMoving;
        public bool isRepath;
        public List<NavAgent> agentNeighbors;
        public List<NavRVOObstacle> obstacleNeighbors;
    }

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
            return (dx + dz) + (NavMathUtils.SQRT2 - 2.0f) * Mathf.Min(dx, dz);
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
            return new Vector2Int
            {
                x = Mathf.Clamp(x - (unitSize >> 1), 0, navMap.XSize - unitSize),
                y = Mathf.Clamp(z - (unitSize >> 1), 0, navMap.ZSize - unitSize),
            };
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
            var squareType = navMap.GetSquareType(x >> 1, z >> 1);
            var slope = navMap.GetSquareSlope(x >> 1, z >> 1);
            var speedMod = agent.moveDef.GetSpeedMod(squareType);
            var slopeMod = agent.moveDef.GetSlopeMod();
            return speedMod / (1.0f + slope * slopeMod);
        }
        public static float GetSquareSpeed(NavMap navMap, NavAgent agent, int x, int z, NavDirection dir)
        {
            return GetSquareSpeed(navMap, agent, x, z);
            //var slope = navMap.GetSquareSlope(x, z);
            //var squareType = navMap.GetSquareType(x, z);
            //var centerNormal2D = navMap.GetSquareCenterNormal2D(x, z);
            //var moveDir = DirToVector3(dir);
            //var dirSlopeMod = -NavMathUtils.Dot2D(moveDir, centerNormal2D);
            //var speedMod = agent.moveDef.GetSpeedMod(squareType);
            //var slopeMod = agent.moveDef.GetSlopeMod();
            //return speedMod / (1.0f + Mathf.Max(0.0f, slope * dirSlopeMod) * slopeMod);
        }
        public static bool IsPushResistant(NavAgent collider, NavAgent collidee)
        {
            if (collider == collidee)
            {
                return false;
            }
            return collidee.param.isPushResistant || collider.param.teamID != collidee.param.teamID;
        }
        public static float CalcAvoidanceWeight(NavAgent collider, NavAgent collidee)
        {
            var pushCollider = !IsPushResistant(collidee, collider);
            var pushCollidee = !IsPushResistant(collider, collidee);
            if (pushCollider)
            {
                if (pushCollidee)
                {
                    return collider.param.mass / (collider.param.mass + collidee.param.mass);
                }
                return 0.0f;
            }
            else if (pushCollidee)
            {
                return 1.0f;
            }
            if (collider.isMoving)
            {
                if (collidee.isMoving)
                {
                    return collider.param.mass / (collider.param.mass + collidee.param.mass);
                }
                return 0.0f;
            }
            else if (collidee.isMoving)
            {
                return 1.0f;
            }
            return collider.id < collidee.id ? 1.0f : 0.0f;
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
        public static bool TestMoveSquareCenter(NavMap navMap, NavAgent agent, int x, int z)
        {
            Debug.Assert(x >= 0 && x < navMap.XSize && z >= 0 && z < navMap.ZSize);

            var squareType = navMap.GetSquareType(x >> 1, z >> 1);
            var speedMod = agent.moveDef.GetSpeedMod(squareType);
            if (speedMod <= 0.0f)
            {
                return false;
            }
            var slope = navMap.GetSquareSlope(x >> 1, z >> 1);
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
        public static NavBlockType TestBlockTypesSquareCenter(NavManager navManager, NavAgent agent, int x, int z)
        {
            Debug.Assert(navManager != null && agent != null);

            var blockTypes = NavBlockType.None;
            foreach (var other in navManager.GetSquareAgents(x, z))
            {
                blockTypes |= TestBlockType(agent, other);
                if ((blockTypes & NavBlockType.Blocked) != 0)
                {
                    break;
                }
            }
            return blockTypes;
        }
        public static NavBlockType TestBlockTypesSquare(NavManager navManager, NavAgent agent, int x, int z)
        {
            Debug.Assert(navManager != null && agent != null);

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
                    blockTypes |= TestBlockTypesSquareCenter(navManager, agent, tx, tz);
                    if ((blockTypes & NavBlockType.Blocked) != 0)
                    {
                        return blockTypes;
                    }
                }
            }
            return blockTypes;
        }
        public static bool IsNoneBlockTypeSquare(NavManager navManager, NavAgent agent, int x, int z)
        {
            Debug.Assert(navManager != null && agent != null);

            var halfUnitSize = (agent.moveDef.GetUnitSize() - 1) >> 1;
            int xmin = x - halfUnitSize;
            int xmax = x + halfUnitSize;
            int zmin = z - halfUnitSize;
            int zmax = z + halfUnitSize;

            for (int tz = zmin; tz <= zmax; tz += 2)
            {
                for (int tx = xmin; tx <= xmax; tx += 2)
                {
                    foreach (var other in navManager.GetSquareAgents(tx, tz))
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