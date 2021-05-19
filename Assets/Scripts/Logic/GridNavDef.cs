using System.Collections.Generic;
using UnityEngine;

namespace GridNav
{
    public static class Constants
    {
        public const int MaxAreaTypes = 32;
        public const int MaxSpeedModMultTypes = 3;
    }

    public enum Direction { None = 0, Left = 1, Right = 2, Up = 3, Down = 4, LeftUp = 5, RightUp = 6, LeftDown = 7, RightDown = 8 }

    public enum MoveState { Idle = 0, Requesting = 1, WaitForPath = 2, InProgress = 3 }

    public enum BlockType { None = 0, Moving = 1, Idle = 2, Busy = 4, Block = 8 };

    public enum SpeedModMultType { Idle = 0, Busy = 1, Move = 2 };

    public class MoveDef
    {
        //speed = (1.0f / (1.0f + slope * slopeMod)) * speedMod * speedModMult
        public float maxSlope = 0.5f; // 最大爬坡角度(0.0f - 1.0f) => (0 - 90)
        public float slopeMod = 0.0f; // 爬坡消耗,值越大，消耗越大 
        public float[] speedMods = new float[Constants.MaxAreaTypes]; //<=0表示不能行走,值越大速度越快
        public float[] speedModMults = new float[Constants.MaxSpeedModMultTypes] { 0.35f, 0.10f, 0.65f }; // 对应SpeedModMultType 值越小，寻路消耗越大
        public bool isAvoidMobilesOnPath = true; // 是否规避移动的单位

        public MoveDef()
        {
            speedMods[0] = 1.0f;
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
        public MoveDef moveDef;
        public int halfUnitSize; //unitSize = 1 + halfUnitSize * 2;
        public float maxInteriorRadius;
        public MoveState moveState;
        public Vector3 pos;
        public int squareIndex;
        public Vector3 goalPos;
        public int goalSquareIndex;
        public float goalRadius;
        public List<Vector3> path;
        public Vector3 prefVelocity;
        public Vector3 velocity;
        public Vector3 newVelocity;
        public bool isMoving;
        public int tempNum;
    }

    class ORCALine
    {
        public Vector3 point;
        public Vector3 direction;
    }

    class ORCAObstacle
    {
        public Vector3 point;
        public Vector3 direction;
        public bool isConvex;
        public ORCAObstacle prev;
        public ORCAObstacle next;
    }
}