using System.Collections.Generic;
using UnityEngine;

public enum GridNavAgentFlags { EnemyPushResistant = 0x01, FriendPushResistant = 0x02 }
public enum GridNavAgentState { NotActive, Walking }
public enum GridNavAgentMoveState { None, Failed, Valid, Requesting, WaitForQueue, WaitForPath }

public struct GridNavAgentParam
{
    public int teamID;
    public float mass;
    public float radius;
    public float maxSpeed;
    public float maxAcc;
    public float maxTurnAngle;
    public int Flags;
}

public class GridNavAgent
{
    public GridNavAgentParam param;
    public GridNavAgentState state;
    public GridNavAgentMoveState moveState;
    public int unitSize;
    public int squareIndex;
    public Vector3 pos;
    public Vector3 targetPos;
    public Vector3 vel;
    public Vector3 desireVel;
}

public class GridNavManager
{
    private GridNavMesh navMesh;
    private GridNavQuery navQuery;
    private List<GridNavAgent> agents;
    private Dictionary<int, List<GridNavAgent>> squareAgents;

    public bool Init(GridNavMesh navMesh, int maxAgents)
    {
        this.navMesh = navMesh;
        this.navQuery = new GridNavQuery();
        if (!navQuery.Init(navMesh))
        {
            return false;
        }
        this.agents = new List<GridNavAgent>();
        this.squareAgents = new Dictionary<int, List<GridNavAgent>>();
        return true;
    }
    public void Clear()
    {
    }
    public GridNavAgent AddAgent(Vector3 pos, GridNavAgentParam param)
    {
        var unitSize = Mathf.Max(1, (int)(param.radius / navMesh.SquareSize + 0.9f));
        var agent = new GridNavAgent
        {
            param = param,
            state = GridNavAgentState.Walking,
            moveState = GridNavAgentMoveState.None,
            unitSize = unitSize,
            squareIndex = 0,
            pos = pos,
            targetPos = Vector3.zero,
            vel = Vector3.zero,
            desireVel = Vector3.zero,
        };
        navMesh.ClampInBounds(agent.pos, out agent.squareIndex, out agent.pos);
        agents.Add(agent);
        return agent;
    }
    public void Update(float deltaTime)
    {
    }
    private void AddSquareAgent(int index, GridNavAgent agent)
    {
        if (!navMesh.GetSquareXZ(index, out var x, out var z))
        {
            return;
        }
    }
    private void RemoveSquareAgent(int index, GridNavAgent agent)
    {
        if (!navMesh.GetSquareXZ(index, out var x, out var z))
        {
            return;
        }
    }
}