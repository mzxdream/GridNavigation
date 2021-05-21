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
        private List<int> pathRequestQueue;
        private NavQuery[] workNavQuerys;

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
            this.pathRequestQueue = new List<int>();
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
            return true;
        }
        public void Clear()
        {
            this.navQuery.Clear();
            for (int i = 0; i < workNavQuerys.Length; ++i)
            {
                var query = workNavQuerys[i];
                if (query != null)
                {
                    query.Clear();
                }
            }
        }
        public void Update(float deltaTime)
        {
            var agentList = new List<NavAgent>(agents.Values);
            NavCrowdUpdate.Update(this, navMap, blockingObjectMap, agentList, pathRequestQueue, workNavQuerys, deltaTime);
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
                pathRequestQueue.Remove(agent.id);
            }
            else if (agent.moveState == NavMoveState.WaitForPath)
            {
                Debug.Assert(pathRequestQueue[0] == agent.id);
                pathRequestQueue.RemoveAt(0);
            }
            blockingObjectMap.RemoveAgent(agent);
            agents.Remove(agentID);
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
                Debug.Assert(agent.id == pathRequestQueue[0]);
                if (neareastIndex == agent.goalSquareIndex && Mathf.Abs(goalRadius - agent.goalRadius) < navMap.SquareSize)
                {
                    return true;
                }
                pathRequestQueue.RemoveAt(0);
            }
            agent.moveState = NavMoveState.Requesting;
            agent.goalPos = nearestPos;
            agent.goalRadius = goalRadius;
            agent.goalSquareIndex = neareastIndex;
            agent.prefVelocity = Vector3.zero;
            pathRequestQueue.Add(agent.id);
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
    }
}