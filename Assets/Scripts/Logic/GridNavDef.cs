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

    public class NavMoveParam
    {
        // speed = speedMod * speedModMult / (1.0f + slope * slopeMod)
        public int unitSize; // >=4的偶数
        public float maxSlope = 0.5f; // 最大爬坡角度(0.0f - 1.0f) => (0 - 90) cos曲线
        public float slopeMod = 0.0f; // 爬坡消耗,值越大，消耗越大 
        public float[] speedMods = new float[NavDef.MaxAreaTypes]; // <=0表示不能行走,值越大速度越快
        public float[] speedModMults = new float[(int)NavSpeedModMultType.NumTypes]; // 对应NavSpeedModMultType 值越小，寻路消耗越大

        public NavMoveParam()
        {
            speedMods[0] = 1.0f;
            speedMods[1] = 0.0f;
            speedModMults[(int)NavSpeedModMultType.Idle] = 0.35f;
            speedModMults[(int)NavSpeedModMultType.Busy] = 0.10f;
            speedModMults[(int)NavSpeedModMultType.Moving] = 0.65f;
        }
    }

    public struct NavAgentParam
    {
        public float mass;
        public float maxSpeed;
        public bool isPushResistant;
    }

    public class NavAgent
    {
        public int id;
        public NavAgentParam param;
        public NavMoveParam moveParam;
        public NavMoveState moveState;
        public int mapPos;
        public Vector3 pos;
        public Vector3 lastPos;
        public Vector3 goalPos;
        public float goalRadius;
        public List<Vector3> path;
        public List<Vector3> corners;
        public Vector3 velocity;
        public Vector3 prefVelocity;
        public Vector3 newVelocity;
        public bool isMoving;
        public bool isRepath;
        public List<NavAgent> agentNeighbors;
        public List<NavRVOObstacle> obstacleNeighbors;
    }
}