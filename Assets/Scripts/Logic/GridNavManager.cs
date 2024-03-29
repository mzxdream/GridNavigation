using System.Collections.Generic;
using UnityEngine;

namespace GridNav
{
    public class NavManager
    {
        private NavMap navMap;
        private NavMoveDef[] moveDefs;
        private Dictionary<int, NavAgent> agents; // TODO 后续做成缓冲池
        private List<NavAgent>[] suqareAgents;
        private int lastAgentID;
        private NavQuery navQuery;
        private NavQuery[] workNavQuerys;
        private NavQuery moveRequestNavQuery;
        private List<int> moveRequestQueue;
        private int pathFindingNodesPerFrame;
        private int frameNum;
        private int framesPerSecond;
        private float frameTime;

        public int FrameNum { get => frameNum; }
        public int FramesPerSecond { get => framesPerSecond; }
        public float FrameTime { get => frameTime; }

        public bool Init(NavMap navMap, NavMoveDef[] moveDefs, int maxAgents = 1024, int maxWorkers = 4, int pathFindingNodesPerFrame = 8192, int framesPerSecond = 30)
        {
            Debug.Assert(navMap != null && moveDefs != null && maxAgents > 0 && maxWorkers > 0 && pathFindingNodesPerFrame > 0 && framesPerSecond > 0);

            this.navMap = navMap;
            this.moveDefs = moveDefs;
            this.agents = new Dictionary<int, NavAgent>();
            this.suqareAgents = new List<NavAgent>[navMap.XSize * navMap.ZSize];
            for (int i = 0; i < this.suqareAgents.Length; i++)
            {
                this.suqareAgents[i] = new List<NavAgent>();
            }
            this.lastAgentID = 0;
            this.navQuery = new NavQuery();
            if (!navQuery.Init(this))
            {
                Debug.LogError("init nav query failed");
                return false;
            }
            this.workNavQuerys = new NavQuery[maxWorkers];
            for (int i = 0; i < maxWorkers; i++)
            {
                var query = new NavQuery();
                if (!query.Init(this))
                {
                    Debug.LogError("init work nav query:" + i + " failed");
                    return false;
                }
                this.workNavQuerys[i] = query;
            }
            this.moveRequestNavQuery = new NavQuery();
            if (!this.moveRequestNavQuery.Init(this))
            {
                Debug.LogError("init move request nav query failed");
                return false;
            }
            this.moveRequestQueue = new List<int>();
            this.pathFindingNodesPerFrame = pathFindingNodesPerFrame;
            this.frameNum = 0;
            this.framesPerSecond = framesPerSecond;
            this.frameTime = 1.0f / framesPerSecond;
            return true;
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
        public int AddAgent(Vector3 pos, NavAgentParam param)
        {
            if (param.moveType < 0 || param.moveType >= moveDefs.Length)
            {
                Debug.LogError("move def:" + param.moveType + " not exists");
                return 0;
            }
            if (param.mass <= NavMathUtils.EPSILON)
            {
                Debug.LogError("mass should not be zero");
                return 0;
            }
            if (param.maxSpeed <= NavMathUtils.EPSILON)
            {
                Debug.LogError("max speed should not be zero");
                return 0;
            }
            var moveDef = moveDefs[param.moveType];
            var agent = new NavAgent
            {
                id = ++lastAgentID,
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
            if (!NavUtils.TestMoveSquare(navMap, agent, x, z) || !NavUtils.IsNoneBlockTypeSquare(this, agent, x, z))
            {
                NavUtils.ForeachNearestSquare(x, z, 20, (int tx, int tz) =>
                {
                    if (tx < 0 || tx >= navMap.XSize || tz < 0 || tz >= navMap.ZSize)
                    {
                        return true;
                    }
                    if (!NavUtils.TestMoveSquare(navMap, agent, tx, tz) || !NavUtils.IsNoneBlockTypeSquare(this, agent, tx, tz))
                    {
                        return true;
                    }
                    agent.pos = navMap.GetSquarePos(tx, tz);
                    return false;
                });
            }
            agents.Add(agent.id, agent);
            agent.mapPos = NavUtils.CalcMapPos(navMap, moveDef.GetUnitSize(), agent.pos);
            AddSquareAgent(agent);
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
            RemoveSquareAgent(agent);
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
        public void UpdateMoveRequest()
        {
            int maxNodes = pathFindingNodesPerFrame;
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
        public void AddSquareAgent(NavAgent agent)
        {
            int unitSize = agent.moveDef.GetUnitSize();
            int xmin = agent.mapPos.x;
            int zmin = agent.mapPos.y;
            int xmax = Mathf.Min(navMap.XSize - 1, xmin + unitSize);
            int zmax = Mathf.Min(navMap.ZSize - 1, zmin + unitSize);
            for (int z = zmin; z < zmax; z++)
            {
                for (int x = xmin; x < xmax; x++)
                {
                    this.suqareAgents[x + z * navMap.XSize].Add(agent);
                }
            }
        }
        public void RemoveSquareAgent(NavAgent agent)
        {
            int unitSize = agent.moveDef.GetUnitSize();
            int xmin = agent.mapPos.x;
            int zmin = agent.mapPos.y;
            int xmax = Mathf.Min(navMap.XSize - 1, xmin + unitSize);
            int zmax = Mathf.Min(navMap.ZSize - 1, zmin + unitSize);
            for (int z = zmin; z < zmax; z++)
            {
                for (int x = xmin; x < xmax; x++)
                {
                    this.suqareAgents[x + z * navMap.XSize].Remove(agent);
                }
            }
        }
        public List<NavAgent> GetSquareAgents(int x, int z)
        {
            Debug.Assert(x >= 0 && x < navMap.XSize && z >= 0 && z < navMap.ZSize);
            return this.suqareAgents[x + z * navMap.XSize];
        }
    }
}