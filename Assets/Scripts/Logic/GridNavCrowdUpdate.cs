using System.Collections.Generic;
using UnityEngine;

namespace GridNav
{
    public static class NavCrowdUpdate
    {
        public static void Update(NavManager navManager, NavMap navMap, NavBlockingObjectMap blockingObjectMap, List<NavAgent> agents, NavQuery[] navQuerys, float deltaTime)
        {
            UpdateMoveRequest(navManager, agents); // 单线程
            UpdatePrefVelocity(navManager, navMap, blockingObjectMap, agents, navQuerys, deltaTime); // 多线程
            UpdateNewVelocity(navManager, navMap, blockingObjectMap, agents, navQuerys, deltaTime); // 多线程
            UpdatePos(navManager, navMap, blockingObjectMap, agents, navQuerys, deltaTime); // 单线程
        }
        private static void UpdateMoveRequest(NavManager navManager, List<NavAgent> agents)
        {
            foreach (var agent in agents)
            {
                if (agent.isRepath)
                {
                    navManager.StartMoving(agent.id, agent.goalPos, agent.goalRadius);
                    agent.isRepath = false;
                }
            }
            navManager.UpdateMoveRequest();
        }
        private static void UpdatePos(NavManager navManager, NavMap navMap, NavBlockingObjectMap blockingObjectMap, List<NavAgent> agents, NavQuery[] navQuerys, float deltaTime)
        {
            var navQuery = navQuerys[0];
            foreach (var agent in agents)
            {
                agent.velocity = agent.newVelocity;
                agent.isMoving = false;
                if (agent.velocity.sqrMagnitude <= NavMathUtils.EPSILON)
                {
                    agent.velocity = Vector3.zero;
                }
                navMap.ClampInBounds(agent.pos + agent.velocity * deltaTime, out var nextSquareIndex, out var nextPos);
                if (!navQuery.FindNearestSquare(agent, nextPos, agent.maxInteriorRadius * 20.0f, out nextSquareIndex, out nextPos))
                {
                    continue;
                }
                if (NavMathUtils.SqrDistance2D(agent.pos, nextPos) <= NavMathUtils.EPSILON)
                {
                    agent.velocity = Vector3.zero;
                }
                else
                {
                    agent.isMoving = true;
                    if (nextSquareIndex != agent.squareIndex)
                    {
                        blockingObjectMap.RemoveAgent(agent);
                        agent.pos = nextPos;
                        agent.squareIndex = nextSquareIndex;
                        blockingObjectMap.AddAgent(agent);
                    }
                    else
                    {
                        agent.pos = nextPos;
                        agent.squareIndex = nextSquareIndex;
                    }
                }
            }
        }
        private static void UpdateNewVelocity(NavManager navManager, NavMap navMap, NavBlockingObjectMap blockingObjectMap, List<NavAgent> agents, NavQuery[] navQuerys, float deltaTime)
        {
            foreach (var agent in agents)
            {
                CollectNeighbors(navMap, blockingObjectMap, agent);
                NavRVO.ComputeNewVelocity(agent, agent.obstacleNeighbors, agent.agentNeighbors, deltaTime);
            }
        }
        private static void UpdatePrefVelocity(NavManager navManager, NavMap navMap, NavBlockingObjectMap blockingObjectMap, List<NavAgent> agents, NavQuery[] navQuerys, float deltaTime)
        {
            var navQuery = navQuerys[0];
            foreach (var agent in agents)
            {
                if (agent.moveState != NavMoveState.InProgress)
                {
                    agent.prefVelocity = Vector3.zero;
                    continue;
                }
                if (NavMathUtils.SqrDistance2D(agent.pos, agent.goalPos) <= agent.goalRadius * agent.goalRadius)
                {
                    agent.prefVelocity = Vector3.zero;
                    agent.moveState = NavMoveState.Idle;
                    continue;
                }
                if (agent.squareIndex == agent.goalSquareIndex)
                {
                    agent.prefVelocity = agent.goalPos - agent.pos;
                    continue;
                }
                while (agent.path.Count > 0 && NavMathUtils.DistanceApproximately(agent.squareIndex, agent.path[0]) <= 5.0f)
                {
                    agent.path.RemoveAt(0);
                }
                var nextSquareIndex = agent.goalSquareIndex;
                var nextSquarePos = agent.goalPos;
                while (agent.path.Count > 0 && NavMathUtils.DistanceApproximately(agent.squareIndex, agent.path[0]) <= 10.0f)
                {
                    if (!NavUtils.IsBlockedRange(navMap, blockingObjectMap, agent, agent.path[0]))
                    {
                        nextSquareIndex = agent.path[0];
                        if (nextSquareIndex != agent.goalSquareIndex)
                        {
                            nextSquarePos = navMap.GetSquarePos(nextSquareIndex);
                        }
                        break;
                    }
                    agent.path.RemoveAt(0);
                }
                var constraint = new NavQueryConstraint(agent, agent.squareIndex, agent.pos, nextSquareIndex, nextSquarePos, 0.0f);
                navQuery.InitSlicedFindPath(constraint);
                navQuery.UpdateSlicedFindPath(128, out _);
                var status = navQuery.FinalizeSlicedFindPath(out var path);
                if ((status & NavQueryStatus.Success) == 0 || (status & NavQueryStatus.Partial) != 0)
                {
                    agent.prefVelocity = Vector3.zero;
                    agent.isRepath = true;
                    continue;
                }
                var nextPos = path[1] == agent.goalSquareIndex ? agent.goalPos : navMap.GetSquarePos(nextSquareIndex);
                agent.prefVelocity = nextPos - agent.pos;
            }
        }
        private static void CollectNeighbors(NavMap navMap, NavBlockingObjectMap blockingObjectMap, NavAgent agent)
        {
            agent.agentNeighbors.Clear();
            agent.obstacleNeighbors.Clear();
            float queryRadius = agent.maxInteriorRadius + agent.param.maxSpeed * 2.0f;
            navMap.GetSquareXZ(new Vector3(agent.pos.x - queryRadius, 0, agent.pos.z - queryRadius), out var sx, out var sz);
            navMap.GetSquareXZ(new Vector3(agent.pos.x + queryRadius, 0, agent.pos.z + queryRadius), out var ex, out var ez);
            for (int z = sz; z <= ez; z++)
            {
                for (int x = sx; x <= ex; x++)
                {
                    var isBlocked = false;
                    if (blockingObjectMap.GetSquareAgents(x, z, out var agentList))
                    {
                        foreach (var other in agentList)
                        {
                            if (other == agent)
                            {
                                continue;
                            }
                            if (!agent.agentNeighbors.Contains(other))
                            {
                                agent.agentNeighbors.Add(other);
                            }
                            if ((NavUtils.TestBlockType(agent, other) & NavBlockType.Block) != 0)
                            {
                                isBlocked = true;
                            }
                        }
                    }
                    if (isBlocked || !NavUtils.TestMoveSquare(navMap, agent, x, z))
                    {
                        List<Vector3> vertices = new List<Vector3>();

                        var point = navMap.GetSquarePos(x, z);
                        vertices.Add(new Vector3(point.x + navMap.SquareSize * 0.5f, 0, point.z + navMap.SquareSize * 0.5f));
                        vertices.Add(new Vector3(point.x - navMap.SquareSize * 0.5f, 0, point.z + navMap.SquareSize * 0.5f));
                        vertices.Add(new Vector3(point.x - navMap.SquareSize * 0.5f, 0, point.z - navMap.SquareSize * 0.5f));
                        vertices.Add(new Vector3(point.x + navMap.SquareSize * 0.5f, 0, point.z - navMap.SquareSize * 0.5f));
                        AddObstacle(vertices, ref agent.obstacleNeighbors);
                    }
                }
            }
        }

        private static int AddObstacle(List<Vector3> vertices, ref List<NavRVOObstacle> obstacles)
        {
            if (vertices.Count < 2)
            {
                return -1;
            }
            int obstacleNo = obstacles.Count;
            for (int i = 0; i < vertices.Count; ++i)
            {
                var obstacle = new NavRVOObstacle();
                obstacle.point = vertices[i];

                if (i != 0)
                {
                    obstacle.prev = obstacles[obstacles.Count - 1];
                    obstacle.prev.next = obstacle;
                }
                if (i == vertices.Count - 1)
                {
                    obstacle.next = obstacles[obstacleNo];
                    obstacle.next.prev = obstacle;
                }

                obstacle.direction = NavMathUtils.Normalized2D(vertices[(i == vertices.Count - 1 ? 0 : i + 1)] - vertices[i]);

                if (vertices.Count == 2)
                {
                    obstacle.isConvex = true;
                }
                else
                {
                    obstacle.isConvex = (NavMathUtils.LeftOf2D(vertices[(i == 0 ? vertices.Count - 1 : i - 1)], vertices[i], vertices[(i == vertices.Count - 1 ? 0 : i + 1)]) >= 0.0f);
                }
                obstacle.id = obstacles.Count;
                obstacles.Add(obstacle);
            }
            return obstacleNo;
        }
    }
}