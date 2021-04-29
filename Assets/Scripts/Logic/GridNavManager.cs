using System;
using System.Collections.Generic;
using UnityEngine;

public enum GridNavAgentFlags { EnemyPushResistant = 0x01, FriendPushResistant = 0x02 }

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

public enum GridNavAgentMoveState { None, Requesting, WaitForPath, Moving }

public class GridNavAgent
{
    public int id;
    public GridNavAgentParam param;
    public GridNavAgentMoveState moveState;
    public int unitSize;
    public int squareIndex;
    public Vector3 pos;
    public bool repath;
    public int targetSquareIndex;
    public Vector3 targetPos;
    public List<int> path;
    public Vector3 disp;
    public Vector3 dvel;
    public Vector3 nvel;
    public Vector3 vel;
}

public class GridNavManager
{
    private GridNavMesh navMesh;
    private GridNavQuery navQuery;
    private int lastAgentID;
    private Dictionary<int, GridNavAgent> agents;
    private Dictionary<int, List<GridNavAgent>> squareAgents;
    private List<int> pathRequestQueue;
    private GridNavQuery pathRequestNavQuery;
    private bool isPathRequesting;

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
        this.pathRequestQueue = new List<int>();
        return true;
    }
    public void Clear()
    {
    }
    public int AddAgent(Vector3 pos, GridNavAgentParam param)
    {
        int unitSize = Mathf.CeilToInt(param.radius / navMesh.SquareSize);
        if ((unitSize & 1) == 0)
        {
            unitSize++;
        }
        var agent = new GridNavAgent
        {
            id = ++lastAgentID,
            param = param,
            moveState = GridNavAgentMoveState.None,
            unitSize = unitSize,
            squareIndex = 0,
            pos = pos,
            targetPos = Vector3.zero,
            disp = Vector3.zero,
            dvel = Vector3.zero,
            nvel = Vector3.zero,
            vel = Vector3.zero,
        };
        navMesh.ClampInBounds(agent.pos, out agent.squareIndex, out agent.pos);
        var filter = new GridNavQueryFilterExtraBlockedCheck(unitSize, (int index) =>
        {
            if (squareAgents.TryGetValue(index, out var squareAgentList))
            {
                foreach (var squareAgent in squareAgentList)
                {
                    return squareAgent != agent;
                }
            }
            return false;
        });
        if (navQuery.FindNearestSquare(filter, agent.pos, agent.param.radius * 20.0f, out var nearestIndex, out var nearesetPos))
        {
            agent.squareIndex = nearestIndex;
            agent.pos = nearesetPos;
        }
        agents.Add(agent.id, agent);
        AddSquareAgent(agent.squareIndex, agent);
        return agent.id;
    }
    public void RemoveAgent(int agentID)
    {
        if (agents.TryGetValue(agentID, out var agent))
        {
            if (agent.moveState != GridNavAgentMoveState.None)
            {
                pathRequestQueue.Remove(agent.id);
                if (agent.moveState == GridNavAgentMoveState.WaitForPath)
                {
                    isPathRequesting = false;
                }
            }
            RemoveSquareAgent(agent.squareIndex, agent);
            agents.Remove(agentID);
        }
    }
    public void Update(float deltaTime)
    {
        foreach (var a in agents)
        {
            var agent = a.Value;
            var filter = new GridNavQueryFilterExtraBlockedCheck(agent.unitSize, (int index) =>
            {
                if (squareAgents.TryGetValue(index, out var squareAgentList))
                {
                    foreach (var squareAgent in squareAgentList)
                    {
                        return squareAgent != agent;
                    }
                }
                return false;
            });
            if (filter.IsBlocked(navMesh, agent.squareIndex))
            {
                if (navQuery.FindNearestSquare(filter, agent.pos, agent.param.radius * 20.0f, out var nearestIndex, out var nearesetPos))
                {
                    agent.squareIndex = nearestIndex;
                    agent.pos = nearesetPos;
                    if (agent.moveState == GridNavAgentMoveState.WaitForPath)
                    {
                        isPathRequesting = false;
                        pathRequestQueue.RemoveAt(0);
                        pathRequestQueue.Add(agent.id);
                    }
                }
                else
                {
                    if (agent.moveState != GridNavAgentMoveState.None)
                    {
                        pathRequestQueue.Remove(agent.id);
                        if (agent.moveState == GridNavAgentMoveState.WaitForPath)
                        {
                            isPathRequesting = false;
                        }
                    }
                    agent.moveState = GridNavAgentMoveState.None;
                }
            }
        }
        int maxNodes = 8192;
        while (pathRequestQueue.Count > 0 && maxNodes > 0)
        {
            var agent = agents[pathRequestQueue[0]];
            if (agent.moveState == GridNavAgentMoveState.Requesting)
            {
                Debug.Assert(!isPathRequesting);
                isPathRequesting = true;
                agent.moveState = GridNavAgentMoveState.WaitForPath;
                var filter = new GridNavQueryFilterUnitSize(agent.unitSize);
                var circleIndex = navMesh.GetSquareCenterIndex(agent.squareIndex, agent.targetSquareIndex);
                var circleRadius = navMesh.DistanceApproximately(agent.squareIndex, circleIndex) * 3.0f + 200.0f;
                var constraint = new GridNavQueryConstraintCircle(agent.targetSquareIndex, agent.param.radius + 0.1f, circleIndex, circleRadius);
                pathRequestNavQuery.InitSlicedFindPath(filter, agent.squareIndex, constraint);
            }
            if (agent.moveState == GridNavAgentMoveState.WaitForPath)
            {
                var status = pathRequestNavQuery.UpdateSlicedFindPath(maxNodes, out var doneNodes);
                maxNodes -= doneNodes;
                if (status != GridNavQueryStatus.InProgress)
                {
                    isPathRequesting = false;
                    pathRequestQueue.RemoveAt(0);
                    if (status == GridNavQueryStatus.Failed)
                    {
                        agent.moveState = GridNavAgentMoveState.None;
                    }
                    else if (status == GridNavQueryStatus.Success)
                    {
                        agent.moveState = GridNavAgentMoveState.Moving;
                        pathRequestNavQuery.FinalizeSlicedFindPath(out agent.path);
                    }
                }
            }
        }
        foreach (var a in agents)
        {
            var agent = a.Value;
            if (agent.moveState != GridNavAgentMoveState.Moving)
            {
                continue;
            }
            Debug.Assert(agent.path.Count > 0);
            while (agent.path.Count > 1 && navMesh.DistanceApproximately(agent.squareIndex, agent.path[0]) <= 10.0f * navMesh.SquareSize)
            {
                agent.path.RemoveAt(0);
            }
            var filter = new GridNavQueryFilterExtraBlockedCheck(agent.unitSize, (int index) =>
            {
                if (squareAgents.TryGetValue(index, out var squareAgentList))
                {
                    foreach (var squareAgent in squareAgentList)
                    {
                        return squareAgent.param.teamID != agent.param.teamID && squareAgent.moveState != GridNavAgentMoveState.Moving;
                    }
                }
                return false;
            });
            bool found = false;
            while (agent.path.Count > 0 && navMesh.DistanceApproximately(agent.squareIndex, agent.path[0]) <= 15.0f * navMesh.SquareSize)
            {
                if (!filter.IsBlocked(navMesh, agent.path[0]))
                {
                    found = true;
                    break;
                }
                agent.path.RemoveAt(0);
            }
            if (!found)
            {
                RequestMoveTarget(agent.id, agent.targetPos);
                continue;
            }
            List<int> path;
            if (!navQuery.Raycast(filter, agent.squareIndex, agent.path[0], out path, out _))
            {
                var constraint = new GridNavQueryConstraintCircle(agent.targetSquareIndex, agent.param.radius + 0.1f, agent.squareIndex, 16.0f * navMesh.SquareSize);
                if (!navQuery.FindPath(filter, agent.squareIndex, constraint, out path))
                {
                    RequestMoveTarget(agent.id, agent.targetPos);
                    continue;
                }
            }
            var nextPos = agent.targetPos;
            if (path.Count > 2)
            {
                nextPos = navMesh.GetSquarePos(path[1]);
            }
            //todo
        }
    }
    public bool RequestMoveTarget(int agentID, Vector3 pos)
    {
        if (!agents.TryGetValue(agentID, out var agent))
        {
            return false;
        }
        navMesh.ClampInBounds(pos, out var neareastIndex, out var nearestPos);
        if (agent.moveState == GridNavAgentMoveState.Requesting)
        {
            agent.targetPos = nearestPos;
            agent.targetSquareIndex = neareastIndex;
            return true;
        }
        if (agent.moveState == GridNavAgentMoveState.WaitForPath)
        {
            Debug.Assert(agent.id == pathRequestQueue[0]);
            if (neareastIndex == agent.targetSquareIndex)
            {
                agent.targetPos = nearestPos;
                return true;
            }
            isPathRequesting = false;
            pathRequestQueue.RemoveAt(0);
        }
        agent.moveState = GridNavAgentMoveState.Requesting;
        agent.targetSquareIndex = neareastIndex;
        agent.targetPos = nearestPos;
        pathRequestQueue.Add(agent.id);
        return true;
    }
    private void AddSquareAgent(int index, GridNavAgent agent)
    {
        navMesh.GetSquareXZ(index, out var x, out var z);
        int xmin = Mathf.Max(0, x - agent.unitSize + 1);
        int xmax = Mathf.Min(navMesh.XSize - 1, x + agent.unitSize - 1);
        int zmin = Mathf.Min(0, z - agent.unitSize + 1);
        int zmax = Mathf.Max(navMesh.ZSize - 1, z + agent.unitSize - 1);
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
        navMesh.GetSquareXZ(index, out var x, out var z);
        int xmin = Mathf.Max(0, x - agent.unitSize + 1);
        int xmax = Mathf.Min(navMesh.XSize - 1, x + agent.unitSize - 1);
        int zmin = Mathf.Min(0, z - agent.unitSize + 1);
        int zmax = Mathf.Max(navMesh.ZSize - 1, z + agent.unitSize - 1);
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
                return squareAgent != agent;
            }
        }
        return false;
    }
}