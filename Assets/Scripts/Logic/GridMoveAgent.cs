using UnityEngine;

public class GridMoveAgentParam
{
    public int teamID; //enemy can't push
    public int unitSize;
    public float mass; //calc push distance
    public float maxSpeed;
    public bool isPushResistant;
}

public class GridMoveAgent
{
    GridMoveManager manager;
    private int id;
    private Vector3 pos;
    private int gridIndex;
    private Vector3 forward;
    private int teamID;
    private int unitSize;
    private float mass;
    private float maxSpeedDef;
    private bool isPushResistant;

    private Vector3 goalPos;
    private float goalRadius;
    private GridPath path;
    private bool isMoving;
    private bool isWantRepath;
    private float curSpeed;
    private float maxSpeed;

    public Vector3 Pos { get => pos; }
    public Vector3 Forward { get => forward; }
    public int UnitSize { get => unitSize; }

    public GridMoveAgent(GridMoveManager manager)
    {
        this.manager = manager;
    }
    public bool Init(int id, Vector3 pos, Vector3 forward, GridMoveAgentParam param)
    {
        this.id = id;
        this.pos = manager.ClampInBounds(pos);
        this.gridIndex = manager.GetGridIndex(this.pos);
        this.forward = forward;
        this.teamID = param.teamID;
        this.unitSize = param.unitSize;
        this.mass = param.mass;
        this.maxSpeedDef = param.maxSpeed;
        this.isPushResistant = param.isPushResistant;


        this.isMoving = false;
        this.isWantRepath = false;
        return true;
    }
    public void Clear()
    {
    }
    public void Update(float deltaTime)
    {
    }
    public void LateUpdate()
    {
    }
    public bool StartMoving(Vector3 goalPos, float goalRadius)
    {
        this.goalPos = goalPos;
        this.goalRadius = goalRadius;
        path = manager.FindPath(this, this.goalPos, this.goalRadius);
        if (path == null)
        {
            return false;
        }
        isMoving = true;
        isWantRepath = false;
        curSpeed = 0;
        maxSpeed = maxSpeedDef;
        return true;
    }
    public void StopMoving()
    {
    }
    public bool IsBlockedOther(GridMoveAgent a)
    {
        return this != a && !isPushResistant && a.teamID == teamID;
    }
}