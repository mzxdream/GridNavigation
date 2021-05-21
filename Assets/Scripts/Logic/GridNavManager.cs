using System.Collections.Generic;
using UnityEngine;

namespace GridNav
{
    public class NavManager
    {
        private NavMap navMap;
        private NavBlockingObjectMap blockingObjectMap;
        private NavQuery navQuery;
        private Dictionary<int, NavAgent> agents; // TODO 后续做成缓冲池
        private int lastAgentID;
        private NavQuery[] workNavQuerys;
        private List<int> moveRequestQueue;
        private NavQuery moveRequestNavQuery;

        public bool Init(NavMap navMap, int maxAgents = 1024, int maxWorkers = 1)
        {
            Debug.Assert(navMap != null && maxAgents > 0 && maxWorkers > 0);
            this.navMap = navMap;
            this.blockingObjectMap = new NavBlockingObjectMap(navMap.XSize, navMap.ZSize);
            this.navQuery = new NavQuery();
            if (!navQuery.Init(navMap, blockingObjectMap))
            {
                return false;
            }
            this.agents = new Dictionary<int, NavAgent>();
            this.lastAgentID = 0;
            this.workNavQuerys = new NavQuery[maxWorkers];
            for (int i = 0; i < maxWorkers; i++)
            {
                var query = new NavQuery();
                if (!query.Init(navMap, blockingObjectMap))
                {
                    return false;
                }
                this.workNavQuerys[i] = query;
            }
            this.moveRequestQueue = new List<int>();
            this.moveRequestNavQuery = new NavQuery();
            if (!this.moveRequestNavQuery.Init(navMap, blockingObjectMap))
            {
                return false;
            }
            return true;
        }
        public void Clear()
        {
            if (navQuery != null)
            {
                navQuery.Clear();
            }
            for (int i = 0; i < workNavQuerys.Length; ++i)
            {
                var query = workNavQuerys[i];
                if (query != null)
                {
                    query.Clear();
                }
            }
            if (moveRequestNavQuery != null)
            {
                moveRequestNavQuery.Clear();
            }
        }
        public void Update(float deltaTime)
        {
            var agentList = new List<NavAgent>(agents.Values);
            NavCrowdUpdate.Update(this, navMap, blockingObjectMap, agentList, workNavQuerys, deltaTime);
        }
        public NavMap GetNavMap()
        {
            return navMap;
        }
        public NavBlockingObjectMap GetBlockingObjectMap()
        {
            return blockingObjectMap;
        }
        public int AddAgent(Vector3 pos, NavAgentParam param, NavMoveParam moveParam)
        {
            var unitSize = NavUtils.CalcUnitSize(param.radius, navMap.SquareSize);
            var agent = new NavAgent
            {
                id = ++lastAgentID,
                param = param,
                moveParam = moveParam,
                halfUnitSize = unitSize >> 1,
                maxInteriorRadius = NavUtils.CalcMaxInteriorRadius(unitSize, navMap.SquareSize),
                moveState = NavMoveState.Idle,
                pos = pos,
                squareIndex = -1,
                goalPos = Vector3.zero,
                goalSquareIndex = -1,
                goalRadius = 0.0f,
                path = new List<int>(),
                prefVelocity = Vector3.zero,
                velocity = Vector3.zero,
                newVelocity = Vector3.zero,
                isMoving = false,
                isRepath = false,
                agentNeighbors = new List<NavAgent>(),
                obstacleNeighbors = new List<NavRVOObstacle>(),
            };
            navMap.ClampInBounds(agent.pos, out agent.squareIndex, out agent.pos);
            if (navQuery.FindNearestSquare(agent, agent.pos, agent.param.radius * 20.0f, out var nearestIndex, out var nearesetPos))
            {
                agent.squareIndex = nearestIndex;
                agent.pos = nearesetPos;
            }
            agents.Add(agent.id, agent);
            blockingObjectMap.AddAgent(agent);
            return agent.id;
        }
        public void RemoveAgent(int agentID)
        {
            if (!agents.TryGetValue(agentID, out var agent))
            {
                return;
            }
            if (agent.moveState == NavMoveState.Requesting)
            {
                moveRequestQueue.Remove(agent.id);
            }
            else if (agent.moveState == NavMoveState.WaitForPath)
            {
                Debug.Assert(moveRequestQueue[0] == agent.id);
                moveRequestQueue.RemoveAt(0);
            }
            blockingObjectMap.RemoveAgent(agent);
            agents.Remove(agentID);
        }
        public NavAgent GetAgent(int agentID)
        {
            return agents.TryGetValue(agentID, out var agent) ? agent : null;
        }
        private bool StartMoving(int agentID, Vector3 goalPos, float goalRadius = 0.0f)
        {
            if (!agents.TryGetValue(agentID, out var agent))
            {
                return false;
            }
            if (goalRadius <= 0.0f)
            {
                goalRadius = agent.param.radius;
            }
            navMap.ClampInBounds(goalPos, out var neareastIndex, out var nearestPos);
            if (agent.moveState == NavMoveState.Requesting)
            {
                agent.goalPos = nearestPos;
                agent.goalRadius = goalRadius;
                return true;
            }
            if (agent.moveState == NavMoveState.WaitForPath)
            {
                Debug.Assert(agent.id == moveRequestQueue[0]);
                if (neareastIndex == agent.goalSquareIndex && Mathf.Abs(goalRadius - agent.goalRadius) < navMap.SquareSize)
                {
                    return true;
                }
                moveRequestQueue.RemoveAt(0);
            }
            agent.moveState = NavMoveState.Requesting;
            agent.goalPos = nearestPos;
            agent.goalRadius = goalRadius;
            agent.goalSquareIndex = neareastIndex;
            agent.prefVelocity = Vector3.zero;
            moveRequestQueue.Add(agent.id);
            return true;
        }
        public bool GetLocation(int agentID, out Vector3 pos, out Vector3 forward)
        {
            pos = Vector3.zero;
            forward = Vector3.zero;
            if (!agents.TryGetValue(agentID, out var agent))
            {
                return false;
            }
            pos = agent.pos;
            forward = agent.velocity.normalized;
            return true;
        }
        public Vector3 GetPrefVelocity(int agentID)
        {
            if (!agents.TryGetValue(agentID, out var agent))
            {
                return Vector3.zero;
            }
            return agent.prefVelocity;
        }
        public Vector3 GetVelocity(int agentID)
        {
            if (!agents.TryGetValue(agentID, out var agent))
            {
                return Vector3.zero;
            }
            return agent.velocity;
        }
        public void UpdateMoveRequest()
        {
            int maxNodes = 10240;
            while (moveRequestQueue.Count > 0 && maxNodes > 0) //寻路
            {
                var agent = agents[moveRequestQueue[0]];
                if (agent.moveState == NavMoveState.Requesting)
                {
                    agent.moveState = NavMoveState.WaitForPath;
                    var circleIndex = navMesh.GetSquareCenterIndex(agent.squareIndex, agent.goalSquareIndex);
                    var circleRadius = navMesh.DistanceApproximately(agent.squareIndex, circleIndex) * 3.0f + 100.0f;
                    var constraint = new GridNavQueryConstraintCircle(agent.goalSquareIndex, agent.goalRadius, circleIndex, circleRadius);
                    pathRequestNavQuery.InitSlicedFindPath(agent.pathFilter, agent.squareIndex, constraint);
                }
                if (agent.state == GridNavAgentState.WaitForPath)
                {
                    Debug.Assert(pathRequestQueue[0] == agent.id);
                    var status = pathRequestNavQuery.UpdateSlicedFindPath(maxNodes, out var doneNodes);
                    maxNodes -= doneNodes;
                    if (status != GridNavQueryStatus.InProgress)
                    {
                        pathRequestQueue.RemoveAt(0);
                        if (status == GridNavQueryStatus.Failed)
                        {
                            agent.state = GridNavAgentState.None;
                        }
                        else if (status == GridNavQueryStatus.Success)
                        {
                            agent.state = GridNavAgentState.Moving;
                            pathRequestNavQuery.FinalizeSlicedFindPath(out agent.path);
                        }
                    }
                }
            }
        }
    }
}