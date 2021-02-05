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
    private Vector3 forward;
    GridMoveAgentParam param;
    private int gridIndex;

    public GridMoveAgent(GridMoveManager manager)
    {
        this.manager = manager;
    }
    public bool Init(int id, Vector3 pos, Vector3 forward, GridMoveAgentParam param)
    {
        this.id = id;
        this.pos = pos;
        this.forward = forward;
        this.param = param;

        manager.ClampInBounds(ref pos);
        this.gridIndex = manager.GetGridIndex(pos);
        return true;
    }
    public void Clear()
    {
    }
    public void Update()
    {
    }
    
    public void LateUpdate()
    {
    }
}