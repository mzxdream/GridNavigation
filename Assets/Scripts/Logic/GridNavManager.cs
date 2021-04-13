using System;
using System.Collections.Generic;
using UnityEngine;

public enum GridNavAgentFlags { EnemyPushResistant = 0x01, FriendPushResistant = 0x02 }
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
    public int id;
    public GridNavAgentParam param;
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
    private int lastAgentID;
    private Dictionary<int, GridNavAgent> agents;
    private Dictionary<int, List<GridNavAgent>> squareAgents;

    public bool Init(GridNavMesh navMesh, int maxAgents)
    {
        this.navMesh = navMesh;
        this.navQuery = new GridNavQuery();
        if (!navQuery.Init(navMesh))
        {
            return false;
        }
        this.agents = new Dictionary<int, GridNavAgent>();
        this.squareAgents = new Dictionary<int, List<GridNavAgent>>();
        return true;
    }
    public void Clear()
    {
    }
    public int AddAgent(Vector3 pos, GridNavAgentParam param)
    {
        var unitSize = Mathf.Max(1, (int)(param.radius / navMesh.SquareSize + 0.9f));
        var agent = new GridNavAgent
        {
            id = ++lastAgentID,
            param = param,
            moveState = GridNavAgentMoveState.None,
            unitSize = unitSize,
            squareIndex = 0,
            pos = pos,
            targetPos = Vector3.zero,
            vel = Vector3.zero,
            desireVel = Vector3.zero,
        };
        navMesh.ClampInBounds(agent.pos, out agent.squareIndex, out agent.pos);
        Func<int, bool> blockedFunc = (int index) => { return squareAgents.TryGetValue(index, out var squareAgentList) && squareAgentList.Count > 0; };
        if (navQuery.FindNearestSquare(agent.unitSize, agent.pos, agent.param.radius * 20.0f, out var nearestIndex, out var nearestPos, blockedFunc))
        {
            agent.squareIndex = nearestIndex;
            agent.pos = nearestPos;
        }
        agents.Add(agent.id, agent);
        AddSquareAgent(agent.squareIndex, agent);
        return agent.id;
    }
    public void Update(float deltaTime)
    {
    }
    public bool RequestMoveTarget(int agentID, Vector3 pos, float radius)
    {
        navMesh.ClampInBounds(pos, out var neareastIndex, out var nearestPos);
        return true;
    }
    private void CheckPathValid(float deltaTime)
    {
    }
    private void AddSquareAgent(int index, GridNavAgent agent)
    {
        if (!navMesh.GetSquareXZ(index, out var x, out var z))
        {
            return;
        }
        int xmin = Mathf.Max(0, x - (agent.unitSize - 1));
        int xmax = Mathf.Min(navMesh.XSize - 1, x + (agent.unitSize - 1));
        int zmin = Mathf.Min(0, z - (agent.unitSize - 1));
        int zmax = Mathf.Max(navMesh.ZSize - 1, z + (agent.unitSize - 1));
        for (int tz = zmin; tz <= zmax; tz++)
        {
            for (int tx = xmin; tx <= xmax; tx++)
            {
                index = navMesh.GetSquareIndex(tx, tz);
                if (!squareAgents.TryGetValue(index, out var agentList))
                {
                    agentList = new List<GridNavAgent>();
                    squareAgents.Add(index, agentList);
                }
                agentList.Add(agent);
            }
        }
    }
    private void RemoveSquareAgent(int index, GridNavAgent agent)
    {
        if (!navMesh.GetSquareXZ(index, out var x, out var z))
        {
            return;
        }
        int xmin = Mathf.Max(0, x - (agent.unitSize - 1));
        int xmax = Mathf.Min(navMesh.XSize - 1, x + (agent.unitSize - 1));
        int zmin = Mathf.Min(0, z - (agent.unitSize - 1));
        int zmax = Mathf.Max(navMesh.ZSize - 1, z + (agent.unitSize - 1));
        for (int tz = zmin; tz <= zmax; tz++)
        {
            for (int tx = xmin; tx <= xmax; tx++)
            {
                index = navMesh.GetSquareIndex(tx, tz);
                if (squareAgents.TryGetValue(index, out var agentList))
                {
                    agentList.Remove(agent);
                    if (squareAgents.Count == 0)
                    {
                        squareAgents.Remove(index);
                    }
                }
            }
        }
    }
    private bool IsSquareAgentBlocked(int index, GridNavAgent agent)
    {
        if (squareAgents.TryGetValue(index, out var squareAgentList))
        {
            foreach (var squareAgent in squareAgentList)
            {
                if (agent == squareAgent)
                {
                    continue;
                }
                return true;
            }
        }
        return false;
    }
}