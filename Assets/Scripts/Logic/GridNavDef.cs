using System.Collections.Generic;
using UnityEngine;

namespace GridNav
{
    public enum NavDirection { None = 0, Forward = 1, Back = 2, Left = 3, Right = 4, LeftForward = 5, RightForward = 6, LeftBack = 7, RightBack = 8 }

    public enum NavDirectionOpt { None = 0, Forward = 1, Back = 2, Left = 4, Right = 8 }

    public enum NavMoveState { Idle = 0, Requesting = 1, WaitForPath = 2, InProgress = 3 }

    public enum NavBlockType { None = 0, Moving = 1, Idle = 2, Busy = 4, Block = 8 };

    public enum NavSpeedModMultType { Idle = 0, Busy = 1, Move = 2 };

    public class NavDef
    {
        public const int MaxAreaTypes = 32;
        public const int MaxSpeedModMultTypes = 3;
    }

    public class NavMoveParam
    {
        // speed = speedMod * speedModMult / (1.0f + slope * slopeMod)
        public float maxSlope = 0.5f; // 最大爬坡角度(0.0f - 1.0f) => (0 - 90)
        public float slopeMod = 0.0f; // 爬坡消耗,值越大，消耗越大 
        public float[] speedMods = new float[NavDef.MaxAreaTypes]; // <=0表示不能行走,值越大速度越快
        public float[] speedModMults = new float[NavDef.MaxSpeedModMultTypes] { 0.35f, 0.10f, 0.65f }; // 对应NavSpeedModMultType 值越小，寻路消耗越大
        public bool isAvoidMobilesOnPath = true; // 是否规避移动的单位

        public NavMoveParam()
        {
            speedMods[0] = 1.0f;
            speedMods[1] = 0.0f;
        }
    }

    public struct NavAgentParam
    {
        public float mass;
        public float radius;
        public float maxSpeed;
        public bool isPushResistant;
    }

    public class NavAgent
    {
        public int id;
        public NavAgentParam param;
        public NavMoveParam moveParam;
        public int halfUnitSize; //unitSize = 1 + halfUnitSize * 2;
        public float maxInteriorRadius;
        public NavMoveState moveState;
        public Vector3 pos;
        public int squareX;
        public int squareZ;
        public Vector3 goalPos;
        public int goalSquareX;
        public int goalSquareZ;
        public float goalRadius;
        public List<Vector3> path;
        public Vector3 prefVelocity;
        public Vector3 velocity;
        public Vector3 newVelocity;
        public bool isMoving;
        public int tempNum;
    }

    public static class NavUtils
    {
        public static int GetSquareIndex(int x, int z)
        {
            return x + z << 16;
        }
        public static void GetSquareXZ(int index, out int x, out int z)
        {
            x = index & 0xFFFF;
            z = index >> 16;
        }
        public static float GetAgentSquareSpeed(NavAgent agent, NavMap navMap, int x, int z)
        {
            int xmin = x - agent.halfUnitSize;
            int xmax = x + agent.halfUnitSize;
            int zmin = z - agent.halfUnitSize;
            int zmax = z + agent.halfUnitSize;
            if (xmin < 0 || xmax >= navMap.XSize || zmin < 0 || zmax >= navMap.ZSize)
            {
                return 0.0f;
            }
            var moveParam = agent.moveParam;
            for (int tz = zmin; tz <= zmax; tz++)
            {
                for (int tx = xmin; tx <= xmax; tx++)
                {
                    if (moveParam.speedMods[navMap.GetSquareType(tx, tz)] <= 0.0f)
                    {
                        return 0.0f;
                    }
                    if (navMap.GetSquareSlope(x, z) > moveParam.maxSlope)
                    {
                        return 0.0f;
                    }
                }
            }
            var slope = navMap.GetSquareSlope(x, z);
            var squareType = navMap.GetSquareType(x, z);
            return moveParam.speedMods[squareType] / (1.0f + slope * moveParam.slopeMod);
        }
        public static float GetAgentSquareSpeed(NavAgent agent, NavMap navMap, int x, int z, Vector3 moveDir)
        {
            int xmin = x - agent.halfUnitSize;
            int xmax = x + agent.halfUnitSize;
            int zmin = z - agent.halfUnitSize;
            int zmax = z + agent.halfUnitSize;
            if (xmin < 0 || xmax >= navMap.XSize || zmin < 0 || zmax >= navMap.ZSize)
            {
                return 0.0f;
            }
            var moveParam = agent.moveParam;
            for (int tz = zmin; tz <= zmax; tz++)
            {
                for (int tx = xmin; tx <= xmax; tx++)
                {
                    if (moveParam.speedMods[navMap.GetSquareType(tx, tz)] <= 0.0f)
                    {
                        return 0.0f;
                    }
                    if (navMap.GetSquareSlope(x, z) > moveParam.maxSlope)
                    {
                        return 0.0f;
                    }
                }
            }
            var slope = navMap.GetSquareSlope(x, z);
            var squareType = navMap.GetSquareType(x, z);
            var centerNormal2D = navMap.GetSquareCenterNormal2D(x, z);
            var dirSlopeMod = -NavMathUtils.Dot2D(NavMathUtils.Normalized2D(moveDir), centerNormal2D);
            return moveParam.speedMods[squareType] / (1.0f + Mathf.Max(0.0f, slope * dirSlopeMod) * moveParam.slopeMod);
        }
    }

    class NavORCALine
    {
        public Vector3 point;
        public Vector3 direction;
    }

    class NavORCAObstacle
    {
        public Vector3 point;
        public Vector3 direction;
        public bool isConvex;
        public NavORCAObstacle prev;
        public NavORCAObstacle next;
    }
}