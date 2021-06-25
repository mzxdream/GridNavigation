using System.Collections.Generic;
using UnityEngine;

namespace GridNav
{
    public class NavManager
    {
        private int frameNum;
        private int framesPerSecond;
        private float frameTime;
        private NavMap navMap;
        private NavBlockingObjectMap blockingObjectMap;
        private NavQuery navQuery;
        private NavQuery[] workNavQuerys;
        private NavQuery moveRequestNavQuery;
        private List<int> moveRequestQueue;
        private Dictionary<int, NavAgent> agents; // TODO 后续做成缓冲池
        private int lastAgentID;
        private NavMoveDef[] moveDefs;

        public int FrameNum { get => frameNum; }
        public int FramesPerSecond { get => framesPerSecond; }
        public float FrameTime { get => frameTime; }

        public bool Init(NavMap navMap, int maxAgents = 1024, int maxMoveDefs = 16, int maxWorkers = 4, int framesPerSecond = 30)
        {
            Debug.Assert(navMap != null && maxAgents > 0 && maxMoveDefs > 0 && maxWorkers > 0 && framesPerSecond > 0);

            this.frameNum = 0;
            this.framesPerSecond = framesPerSecond;
            this.frameTime = 1.0f / framesPerSecond;
            this.navMap = navMap;
            this.blockingObjectMap = new NavBlockingObjectMap(navMap.XSize, navMap.ZSize);
            this.navQuery = new NavQuery();
            if (!navQuery.Init(navMap, blockingObjectMap))
            {
                return false;
            }
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
            this.moveRequestNavQuery = new NavQuery();
            if (!this.moveRequestNavQuery.Init(navMap, blockingObjectMap))
            {
                return false;
            }
            this.moveRequestQueue = new List<int>();
            this.agents = new Dictionary<int, NavAgent>();
            this.lastAgentID = 0;
            this.moveDefs = new NavMoveDef[maxMoveDefs];
            for (int i = 0; i < this.moveDefs.Length; i++)
            {
                this.moveDefs[i] = new NavMoveDef();
            }
            return true;
        }
        public bool AfterInit()
        {
            // TODO
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
        public void Update()
        {
            frameNum++;
            var agentList = new List<NavAgent>(agents.Values);
            NavCrowdUpdate.Update(this, workNavQuerys, agentList);
        }
        public NavMap GetNavMap()
        {
            return navMap;
        }
        public NavBlockingObjectMap GetBlockingObjectMap()
        {
            return blockingObjectMap;
        }
        public NavQuery GetNavQuery()
        {
            return navQuery;
        }
        public NavMoveDef GetMoveDef(int type)
        {
            Debug.Assert(type >= 0 && type < moveDefs.Length);
            return moveDefs[type];
        }
        public int AddAgent(Vector3 pos, NavAgentParam param)
        {
            var moveDef = GetMoveDef(param.moveType);
            Debug.Assert(moveDef != null);
            lastAgentID++;
            var agent = new NavAgent
            {
                id = lastAgentID,
                param = param,
                moveDef = moveDef,
                pos = pos,
                radius = NavUtils.CalcMaxInteriorRadius(moveDef.GetUnitSize(), navMap.SquareSize),
                mapPos = new Vector2Int(-1, -1),
                moveState = NavMoveState.Idle,
                lastPos = pos,
                goalPos = Vector3.zero,
                goalRadius = 0.0f,
                path = null,
                velocity = Vector3.zero,
                prefVelocity = Vector3.zero,
                newVelocity = Vector3.zero,
                isMoving = false,
                isRepath = false,
                agentNeighbors = new List<NavAgent>(),
                obstacleNeighbors = new List<NavRVOObstacle>(),
            };
            agent.param.maxSpeed /= framesPerSecond;
            navMap.ClampInBounds(agent.pos, out var x, out var z, out agent.pos);
            if (!NavUtils.TestMoveSquare(navMap, agent, x, z) || !NavUtils.IsNoneBlockTypeSquare(blockingObjectMap, agent, x, z))
            {
                NavUtils.ForeachNearestSquare(x, z, 20, (int tx, int tz) =>
                {
                    if (tx < 0 || tx >= navMap.XSize || tz < 0 || tz >= navMap.ZSize)
                    {
                        return true;
                    }
                    if (!NavUtils.TestMoveSquare(navMap, agent, tx, tz) || !NavUtils.IsNoneBlockTypeSquare(blockingObjectMap, agent, tx, tz))
                    {
                        return true;
                    }
                    agent.pos = navMap.GetSquarePos(tx, tz);
                    return false;
                });
            }
            agents.Add(agent.id, agent);
            agent.mapPos = NavUtils.CalcMapPos(navMap, moveDef.GetUnitSize(), agent.pos);
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
            agent.moveState = NavMoveState.Idle;
            blockingObjectMap.RemoveAgent(agent);
            agents.Remove(agentID);
        }
        public NavAgent GetAgent(int agentID)
        {
            return agents.TryGetValue(agentID, out var agent) ? agent : null;
        }
        public bool StartMoving(int agentID, Vector3 goalPos, float goalRadius = 0.0f)
        {
            if (!agents.TryGetValue(agentID, out var agent))
            {
                return false;
            }
            if (goalRadius <= 0.0f)
            {
                goalRadius = agent.radius;
            }
            navMap.ClampInBounds(goalPos, out var x, out var z, out var nearestPos);
            if (agent.moveState == NavMoveState.Requesting)
            {
                agent.goalPos = nearestPos;
                agent.goalRadius = goalRadius;
                return true;
            }
            if (agent.moveState == NavMoveState.WaitForPath)
            {
                Debug.Assert(agent.id == moveRequestQueue[0]);
                navMap.GetSquareXZ(agent.goalPos, out var oldX, out var oldZ);
                if (x == oldX && z == oldZ && Mathf.Abs(goalRadius - agent.goalRadius) < navMap.SquareSize)
                {
                    return true;
                }
                moveRequestQueue.RemoveAt(0);
            }
            agent.moveState = NavMoveState.Requesting;
            agent.goalPos = nearestPos;
            agent.goalRadius = goalRadius;
            agent.prefVelocity = Vector3.zero;
            moveRequestQueue.Add(agent.id);
            return true;
        }
        public void StopMoving(int agentID)
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
            agent.moveState = NavMoveState.Idle;
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
            forward = NavMathUtils.Normalized2D(agent.velocity);
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
        public void UpdateMoveRequest(int maxNodes = 1024)
        {
            while (moveRequestQueue.Count > 0 && maxNodes > 0) //寻路
            {
                var agent = agents[moveRequestQueue[0]];
                if (agent.moveState == NavMoveState.Requesting)
                {
                    agent.moveState = NavMoveState.WaitForPath;
                    moveRequestNavQuery.InitSlicedFindPath(agent, agent.pos, agent.goalPos, agent.goalRadius);
                }
                if (agent.moveState == NavMoveState.WaitForPath)
                {
                    Debug.Assert(moveRequestQueue[0] == agent.id);
                    var status = moveRequestNavQuery.UpdateSlicedFindPath(maxNodes, out var doneNodes);
                    maxNodes -= doneNodes;
                    if ((status & NavQueryStatus.InProgress) == 0)
                    {
                        moveRequestQueue.RemoveAt(0);
                        if ((status & NavQueryStatus.Failed) != 0)
                        {
                            agent.moveState = NavMoveState.Idle;
                        }
                        else if ((status & NavQueryStatus.Success) != 0)
                        {
                            agent.moveState = NavMoveState.InProgress;
                            moveRequestNavQuery.FinalizeSlicedFindPath(out agent.path);
                        }
                        else
                        {
                            Debug.Assert(false, "path query status is wrong");
                        }
                    }
                }
            }
        }
    }
}