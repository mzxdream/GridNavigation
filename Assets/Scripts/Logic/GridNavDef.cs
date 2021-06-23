using System.Collections.Generic;
using UnityEngine;

namespace GridNav
{
    public enum NavDirection { None = 0, Forward = 1, Back = 2, Left = 3, Right = 4, LeftForward = 5, RightForward = 6, LeftBack = 7, RightBack = 8 }
    public enum NavMoveState { Idle = 0, Requesting = 1, WaitForPath = 2, InProgress = 3 }
    public enum NavBlockType { None = 0, Idle = 1, Busy = 2, Moving = 4, Structure = 8 };
    public enum NavSpeedModMultType { Idle = 0, Busy = 1, Moving = 2, NumTypes = 3 };

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
            speedMods[0] = 1.0f;
            speedMods[1] = 0.0f;
            speedModMults[(int)NavSpeedModMultType.Idle] = 0.35f;
            speedModMults[(int)NavSpeedModMultType.Busy] = 0.10f;
            speedModMults[(int)NavSpeedModMultType.Moving] = 0.65f;
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
        public List<Vector3> path;
        public Vector3 velocity;
        public Vector3 prefVelocity;
        public Vector3 newVelocity;
        public bool isMoving;
        public bool isRepath;
        public List<NavAgent> agentNeighbors;
        public List<NavRVOObstacle> obstacleNeighbors;
    }
}