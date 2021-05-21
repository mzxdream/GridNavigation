using System.Collections.Generic;
using UnityEngine;

namespace GridNav
{
    public static class NavCrowdUpdate
    {
        public static void Update(NavManager navManager, NavMap navMap, NavBlockingObjectMap blockingObjectMap, List<NavAgent> agents, List<int> pathRequestQueue, NavQuery[] navQuerys, float deltaTime)
        {
            // TODO 后续改成多线程
            //foreach (var a in agents) //移到合法点
            //{
            //    var agent = a.Value;
            //    if (agent.filter.IsBlocked(navMesh, agent.squareIndex))
            //    {
            //        if (navQuery.FindNearestSquare(agent.filter, agent.pos, agent.param.radius * 20.0f, out var nearestIndex, out var nearesetPos))
            //        {
            //            RemoveSquareAgent(agent.squareIndex, agent);
            //            AddSquareAgent(nearestIndex, agent);
            //            agent.squareIndex = nearestIndex;
            //            agent.pos = nearesetPos;
            //            if (agent.state == GridNavAgentState.WaitForPath)
            //            {
            //                Debug.Assert(pathRequestQueue[0] == agent.id);
            //                pathRequestQueue.RemoveAt(0);
            //                pathRequestQueue.Add(agent.id);
            //            }
            //        }
            //        else
            //        {
            //            if (agent.state == GridNavAgentState.WaitForPath)
            //            {
            //                Debug.Assert(pathRequestQueue[0] == agent.id);
            //                pathRequestQueue.RemoveAt(0);
            //            }
            //            else if (agent.state == GridNavAgentState.Requesting)
            //            {
            //                pathRequestQueue.Remove(agent.id);
            //            }
            //            agent.state = GridNavAgentState.None;
            //        }
            //    }
            //}
            //int maxNodes = 10240;
            //while (pathRequestQueue.Count > 0 && maxNodes > 0) //寻路
            //{
            //    var agent = agents[pathRequestQueue[0]];
            //    if (agent.state == GridNavAgentState.Requesting)
            //    {
            //        agent.state = GridNavAgentState.WaitForPath;
            //        var circleIndex = navMesh.GetSquareCenterIndex(agent.squareIndex, agent.goalSquareIndex);
            //        var circleRadius = navMesh.DistanceApproximately(agent.squareIndex, circleIndex) * 3.0f + 100.0f;
            //        var constraint = new GridNavQueryConstraintCircle(agent.goalSquareIndex, agent.goalRadius, circleIndex, circleRadius);
            //        pathRequestNavQuery.InitSlicedFindPath(agent.pathFilter, agent.squareIndex, constraint);
            //    }
            //    if (agent.state == GridNavAgentState.WaitForPath)
            //    {
            //        Debug.Assert(pathRequestQueue[0] == agent.id);
            //        var status = pathRequestNavQuery.UpdateSlicedFindPath(maxNodes, out var doneNodes);
            //        maxNodes -= doneNodes;
            //        if (status != GridNavQueryStatus.InProgress)
            //        {
            //            pathRequestQueue.RemoveAt(0);
            //            if (status == GridNavQueryStatus.Failed)
            //            {
            //                agent.state = GridNavAgentState.None;
            //            }
            //            else if (status == GridNavQueryStatus.Success)
            //            {
            //                agent.state = GridNavAgentState.Moving;
            //                pathRequestNavQuery.FinalizeSlicedFindPath(out agent.path);
            //            }
            //        }
            //    }
            //}
            //foreach (var a in agents) //更改方向和移动速度
            //{
            //    var agent = a.Value;
            //    agent.pos.y = 0.0f;
            //    if (agent.state != GridNavAgentState.Moving)
            //    {
            //        agent.prefVelocity = Vector3.zero;
            //        continue;
            //    }
            //    Debug.Assert(agent.path.Count > 0);
            //    var pathSquareIndex = agent.path[agent.path.Count - 1];
            //    var goalPos = pathSquareIndex == agent.goalSquareIndex ? agent.goalPos : navMesh.GetSquarePos(pathSquareIndex);
            //    if (GridNavMath.SqrDistance2D(agent.pos, goalPos) <= agent.goalRadius * agent.goalRadius)
            //    {
            //        if (pathSquareIndex == agent.goalSquareIndex)
            //        {
            //            agent.prefVelocity = Vector3.zero;
            //            agent.state = GridNavAgentState.None;
            //        }
            //        else
            //        {
            //            StartMoving(agent, agent.goalPos, agent.goalRadius);
            //        }
            //        continue;
            //    }
            //    while (agent.path.Count > 1 && navMesh.DistanceApproximately(agent.squareIndex, agent.path[0]) <= 10.0f * navMesh.SquareSize)
            //    {
            //        agent.path.RemoveAt(0);
            //    }
            //    bool foundNextWayPoint = false;
            //    while (agent.path.Count > 0 && navMesh.DistanceApproximately(agent.squareIndex, agent.path[0]) <= 15.0f * navMesh.SquareSize)
            //    {
            //        if (!agent.pathFilter.IsBlocked(navMesh, agent.path[0]))
            //        {
            //            foundNextWayPoint = true;
            //            break;
            //        }
            //        agent.path.RemoveAt(0);
            //    }
            //    if (!foundNextWayPoint)
            //    {
            //        agent.prefVelocity = Vector3.zero;
            //        StartMoving(agent, agent.goalPos, agent.goalRadius);
            //        continue;
            //    }
            //    agent.path2 = null;
            //    var nextSquareIndex = agent.squareIndex;
            //    if (agent.path[0] != agent.squareIndex)
            //    {
            //        if (!navQuery.Raycast(agent.pathFilter, agent.squareIndex, agent.path[0], out var path, out var totalCost))
            //        {
            //            var constraint = new GridNavQueryConstraintCircleStrict(agent.path[0], agent.squareIndex, 16.0f * navMesh.SquareSize);
            //            if (!navQuery.FindPath(agent.pathFilter, agent.squareIndex, constraint, out path) || path[path.Count - 1] != agent.path[0])
            //            {
            //                agent.prefVelocity = Vector3.zero;
            //                StartMoving(agent, agent.goalPos, agent.goalRadius);
            //                continue;
            //            }
            //            navQuery.FindStraightPath(agent.pathFilter, path, out var straightPath);
            //            nextSquareIndex = path[1];
            //        }
            //        else
            //        {
            //            nextSquareIndex = path[path.Count - 1];
            //        }
            //        agent.path2 = path;
            //    }
            //    var nextPos = nextSquareIndex == agent.goalSquareIndex ? agent.goalPos : navMesh.GetSquarePos(nextSquareIndex);
            //    var disiredDir = GridNavMath.Normalized2D(nextPos - agent.pos);
            //    agent.prefVelocity = disiredDir * agent.param.maxSpeed;
            //}
            //foreach (var a in agents)
            //{
            //    var agent = a.Value;
            //    if (agent.state != GridNavAgentState.Moving)
            //    {
            //        agent.newVelocity = Vector3.zero;
            //        continue;
            //    }
            //    CollectNeighbors(agent);
            //    ComputeNewVelocity(agent, deltaTime);
            //}
            ////更新坐标
            //foreach (var a in agents)
            //{
            //    var agent = a.Value;
            //    agent.velocity = agent.newVelocity;
            //    if (agent.velocity.y > 0.0f)
            //    {
            //        var t = 1;
            //        t++;
            //    }
            //    if (agent.velocity.sqrMagnitude > 0.0001f)
            //    {
            //        agent.frontDir = agent.velocity.normalized;
            //        if (agent.frontDir.y > 0.0f)
            //        {
            //            int t = 1;
            //            t++;
            //        }
            //    }
            //    else
            //    {
            //        agent.velocity = Vector3.zero;
            //        continue;
            //    }
            //    navMesh.ClampInBounds(agent.pos + agent.velocity * deltaTime, out var nextSquareIndex, out var nextPos);
            //    if (agent.filter.IsBlocked(navMesh, nextSquareIndex))
            //    {
            //        if (navQuery.FindNearestSquare(agent.filter, agent.pos, agent.param.radius * 20.0f, out var nearestIndex, out var nearesetPos))
            //        {
            //            nextPos = nearesetPos;
            //        }
            //        else
            //        {
            //            nextPos = agent.pos;
            //        }
            //    }
            //    //if (!navQuery.Raycast(agent.filter, agent.squareIndex, nextSquareIndex, out var path, out var totalCost))
            //    //{
            //    //    if (path.Count > 0 && path[path.Count - 1] != agent.squareIndex)
            //    //    {
            //    //        nextPos = navMesh.GetSquarePos(path[path.Count - 1]);
            //    //    }
            //    //    else
            //    //    {
            //    //        nextPos = agent.pos;
            //    //    }
            //    //}
            //    if (GridNavMath.SqrDistance2D(agent.pos, nextPos) <= 1e-4f)
            //    {
            //        agent.velocity = Vector3.zero;
            //    }
            //    agent.pos = nextPos;
            //    var newSquareIndex = navMesh.GetSquareIndex(agent.pos);
            //    if (newSquareIndex != agent.squareIndex)
            //    {
            //        RemoveSquareAgent(agent.squareIndex, agent);
            //        AddSquareAgent(newSquareIndex, agent);
            //        agent.squareIndex = newSquareIndex;
            //    }
            //}
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