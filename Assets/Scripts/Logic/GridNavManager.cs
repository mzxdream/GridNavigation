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
    public Vector3 frontDir;
    public bool repath;
    public int targetSquareIndex;
    public Vector3 targetPos;
    public List<int> path;
    public Vector3 velocity;
    public float speed;
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
                if (!navQuery.FindPath(filter, agent.squareIndex, constraint, out path) || path[path.Count - 1] != agent.path[0])
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
            agent.dvel = (nextPos - agent.pos).normalized * agent.param.maxSpeed;
            //separate
        }
        foreach (var a in agents)
        {
            var agent = a.Value;
            agent.nvel = agent.dvel;

            var maxDelta = agent.param.maxAcc * deltaTime;
            var dv = agent.nvel - agent.vel;
            var ds = dv.magnitude;
            if (ds > agent.param.maxAcc)
            {
                dv = dv * maxDelta / ds;
            }
            agent.vel = agent.vel + dv;
            if (agent.vel.magnitude > 0.0001f)
            {
                agent.pos = agent.pos + agent.vel * deltaTime;
            }
            else
            {
                agent.vel.Set(0, 0, 0);
            }
        }
        foreach (var a in agents)
        {
            var agent = a.Value;
            agent.nneis = new List<GridNavAgent>();
            float radius = agent.param.radius + agent.param.maxSpeed * 2.0f;
            navMesh.GetSquareXZ(new Vector3(agent.pos.x - radius, 0, agent.pos.z - radius), out var sx, out var sz);
            navMesh.GetSquareXZ(new Vector3(agent.pos.x + radius, 0, agent.pos.z + radius), out var ex, out var ez);
            for (int z = sz; z <= ez; z++)
            {
                for (int x = sx; x <= ex; x++)
                {
                    int index = navMesh.GetSquareIndex(x, z);
                    if (!squareAgents.TryGetValue(index, out var agentList))
                    {
                        continue;
                    }
                    foreach (var t in agentList)
                    {
                        if (!agent.nneis.Contains(t))
                        {
                            agent.nneis.Add(t);
                        }
                    }
                }
            }
        }
    }
    public Vector3 GetObstacleAvoidanceDir(GridNavAgent avoider, Vector3 desiredDir)
    {
        if (Vector3.Dot(avoider.frontDir, desiredDir) < 0.0f)
        {
            return desiredDir;
        }
        Vector3 avoidanceVec = Vector3.zero;
        Vector3 avoidanceDir = desiredDir;

        float MAX_AVOIDEE_COSINE = Mathf.Cos(120.0f * Mathf.Deg2Rad);

        float avoidanceRadius = Mathf.Max(avoider.param.maxSpeed, 1.0f) * (avoider.param.radius * 2.0f);
        float avoiderRadius = Mathf.Sqrt(2) * avoider.unitSize * 0.5f * navMesh.SquareSize;

        foreach (var a in agents)
        {
            var avoidee = a.Value;
            if (avoidee == avoider)
            {
                continue;
            }
            if (Vector3.Distance(avoider.pos, avoidee.pos) > avoidanceRadius)
            {
                continue;
            }
            Vector3 avoideeVector = (avoider.pos + avoider.velocity) - (avoidee.pos + avoidee.velocity);
            float avoideeRadius = Mathf.Sqrt(2) * avoidee.unitSize * 0.5f * navMesh.SquareSize;
            float avoidanceRadiusSum = avoiderRadius + avoideeRadius;
            float avoidanceMassSum = avoider.param.mass + avoidee.param.mass;
            float avoideeMassScale = avoidee.param.mass / avoidanceMassSum;
            float avoideeDistSq = avoideeVector.sqrMagnitude;
            float avoideeDist = Mathf.Sqrt(avoideeDistSq) + 0.01f;

            if (Vector3.Dot(avoider.frontDir, -(avoideeVector / avoideeDist)) < MAX_AVOIDEE_COSINE)
            {
                continue;
            }
            if (avoideeDist >= Mathf.Max(avoider.param.maxSpeed, 1.0f) + avoidanceRadiusSum)
            {
                continue;
            }
            if (avoideeDistSq >= (avoider.pos - avoider.targetPos).sqrMagnitude)
            {
                continue;
            }

            var avoiderRightDir = Vector3.Cross(avoider.frontDir, Vector3.up);
            var avoideeRightDir = Vector3.Cross(avoidee.frontDir, Vector3.up);
            float avoiderTurnSign = Vector3.Dot(avoidee.pos, avoiderRightDir) - Vector3.Dot(avoider.pos, avoiderRightDir) > 0.0f ? -1.0f : 1.0f;
            float avoideeTurnSign = Vector3.Dot(avoider.pos, avoideeRightDir) - Vector3.Dot(avoidee.pos, avoideeRightDir) > 0.0f ? -1.0f : 1.0f;

            float avoidanceCosAngle = Mathf.Clamp(Vector3.Dot(avoider.frontDir, avoidee.frontDir), -1.0f, 1.0f);
            float avoidanceResponse = (1.0f - avoidanceCosAngle) + 0.1f;
            float avoidanceFallOff = (1.0f - Mathf.Min(1.0f, avoideeDist / (5.0f * avoidanceRadiusSum)));

            if (avoidanceCosAngle < 0.0f)
            {
                avoiderTurnSign = Mathf.Max(avoiderTurnSign, avoideeTurnSign);
            }
            avoidanceDir = avoiderRightDir * 1.0f * avoiderTurnSign;
            avoidanceVec += (avoidanceDir * avoidanceResponse * avoidanceFallOff * avoideeMassScale);
        }
        avoidanceDir = Vector3.Lerp(desiredDir, avoidanceVec, 0.5f).normalized;
        avoidanceDir = Vector3.Lerp(avoidanceDir, desiredDir, 0.7f).normalized;
        return avoidanceDir;
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