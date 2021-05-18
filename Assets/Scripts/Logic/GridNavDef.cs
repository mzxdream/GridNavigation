using System.Collections.Generic;
using UnityEngine;

public struct GridNavVector2d : IEqualityComparer<GridNavVector2d>
{
    public short x;
    public short y;

    public bool Equals(GridNavVector2d a, GridNavVector2d b)
    {
        return a.x == b.x && a.y == b.y;
    }
    public int GetHashCode(GridNavVector2d a)
    {
        return ((int)a.x << 16 + a.y).GetHashCode();
    }
}

public class GridNavMoveDef
{
    //speed = (1.0f / (1.0f + slope * slopeMod)) * speedMod * speedModMult
    public enum SpeedModMultType { Idle = 0, Busy = 1, Move = 2 };
    public const int MaxAreas = 32;

    public float maxSlope = 1.0f; // 最大爬坡角度(0.0f - 1.0f) => (0 - 90)
    public float slopeMod = 0.0f; // 爬坡消耗,值越大，消耗越大 
    public float[] speedMods = new float[MaxAreas]; //0表示不能行走
    public float[] speedModMults = new float[] { 0.35f, 0.10f, 0.65f }; // 对应GridNavSpeedModMults 值越小，寻路消耗越大
    public bool avoidMobilesOnPath = true; // 是否规避移动的单位
}

public struct GridNavAgentParam
{
    public int moveType;
    public float mass;
    public float radius;
    public float maxSpeed;
}

public class GridNavAgent
{
    public enum MoveState { Idle = 0, Requesting = 1, WaitForPath = 2, Moving = 3 }

    public int id;
    public GridNavAgentParam param;
    public int unitSize;
    public float radius;
    public MoveState moveState;
    public Vector3 pos;
    public GridNavVector2d squareIndex;
    public Vector3 goalPos;
    public GridNavVector2d goalSquareIndex;
    public float goalRadius;
    public List<Vector3> path;
    public Vector3 prefVelocity;
    public Vector3 velocity;
    public Vector3 newVelocity;
    public int tempNum;
}

class GridNavORCALine
{
    public Vector3 point;
    public Vector3 direction;
}

class GridNavORCAObstacle
{
    public Vector3 point;
    public Vector3 direction;
    public bool isConvex;
    public GridNavORCAObstacle prev;
    public GridNavORCAObstacle next;
}

public enum GridNavBlockType { None = 0, Moving = 1, Idle = 2, Busy = 4, Block = 8 };