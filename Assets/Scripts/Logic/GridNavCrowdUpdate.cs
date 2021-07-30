using System.Collections.Generic;
using UnityEngine;

namespace GridNav
{
    public static class NavCrowdUpdate
    {
        public static void Update(NavManager navManager, NavQuery[] navQueries, List<NavAgent> agents)
        {
            //var t1 = Time.realtimeSinceStartup;
            UpdatePath(navManager, navQueries, agents); //多线程
            //var t2 = Time.realtimeSinceStartup;
            UpdateMoveRequest(navManager, navQueries, agents); // 单线程
            UpdateTopologyOptimization(navManager, navQueries, agents); //多线程
            //var t3 = Time.realtimeSinceStartup;
            UpdateDesiredVelocity(navManager, navQueries, agents); // 多线程
            //var t4 = Time.realtimeSinceStartup;
            UpdateNewVelocity(navManager, navQueries, agents); // 多线程
            //var t5 = Time.realtimeSinceStartup;
            UpdatePos(navManager, navQueries, agents); // 多线程
            UpdateCollision(navManager, navQueries, agents); // 多线程
            UpdateMove(navManager, navQueries, agents);
            //var t6 = Time.realtimeSinceStartup;
            //if (t6 - t1 > 0.001f)
            //{
            //    Debug.Log("Update use totalTime:" + (t6 - t1) + " updatePath:" + (t2 - t1) + " updateMoveRequest:" + (t3 - t2) + " updatePrefVelocity:" + (t4 - t3) + " updateNewVelocity:" + (t5 - t4) + " updatePos:" + (t6 - t5));
            //}
        }
        private static void UpdatePath(NavManager navManager, NavQuery[] navQueries, List<NavAgent> agents)
        {
            var navQuery = navQueries[0];
            var navMap = navManager.GetNavMap();
            foreach (var agent in agents)
            {
                if (agent.moveState != NavMoveState.InProgress)
                {
                    continue;
                }
                int checkStartIndex = 1;
                int checkEndIndex = Mathf.Min(agent.path.Count, 8);
                while (checkStartIndex <= checkEndIndex)
                {
                    var pos = agent.path[agent.path.Count - checkStartIndex];
                    navMap.GetSquareXZ(pos, out var tx, out var tz);
                    if (!NavUtils.TestMoveSquare(navMap, agent, tx, tz))
                    {
                        break;
                    }
                    checkStartIndex++;
                }
                if (checkStartIndex > checkEndIndex)
                {
                    continue;
                }
                checkStartIndex++;
                checkEndIndex = Mathf.Min(checkStartIndex + 20, agent.path.Count);
                while (checkStartIndex <= checkEndIndex)
                {
                    var pos = agent.path[agent.path.Count - checkStartIndex];
                    navMap.GetSquareXZ(pos, out var tx, out var tz);
                    if (NavUtils.TestMoveSquare(navMap, agent, tx, tz))
                    {
                        break;
                    }
                    checkStartIndex++;
                }
                if (checkStartIndex > checkEndIndex)
                {
                    ReRequestPath(agent);
                    continue;
                }
                int index = agent.path.Count - checkStartIndex;
                navQuery.InitSlicedFindPath(agent, agent.pos, agent.path[index], navMap.SquareSize * 0.5f);
                var status = navQuery.UpdateSlicedFindPath(128, out _);
                if ((status & NavQueryStatus.Success) == 0 || (status & NavQueryStatus.Partial) != 0)
                {
                    ReRequestPath(agent);
                    continue;
                }
                navQuery.FinalizeSlicedFindPathWithSimpleSmooth(out var path);
                ReplacePathStart(ref agent.path, index, path);
            }
        }
        private static void UpdateMoveRequest(NavManager navManager, NavQuery[] navQueries, List<NavAgent> agents)
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
        private static void UpdateTopologyOptimization(NavManager navManager, NavQuery[] navQueries, List<NavAgent> agents)
        {
            float optTime = 15;
            NavAgent optAgent = null;
            foreach (var agent in agents)
            {
                if (agent.moveState != NavMoveState.InProgress)
                {
                    continue;
                }
                agent.topologyOptTime++;
                if (agent.topologyOptTime > optTime)
                {
                    optTime = agent.topologyOptTime;
                    optAgent = agent;
                }
            }
            if (optAgent != null)
            {
                optAgent.topologyOptTime = 0;

                var navMap = navManager.GetNavMap();
                var navQuery = navQueries[0];
                int index = 1;
                float distanceMinSqr = NavMathUtils.Square(10.0f * navMap.SquareSize);
                while (index < optAgent.path.Count && NavMathUtils.SqrDistance2D(optAgent.pos, optAgent.path[optAgent.path.Count - index]) < distanceMinSqr)
                {
                    index++;
                }
                float distanceMaxSqr = NavMathUtils.Square(15.0f * navMap.SquareSize);
                while (index <= optAgent.path.Count)
                {
                    var pos = optAgent.path[optAgent.path.Count - index];
                    if (NavMathUtils.SqrDistance2D(optAgent.pos, pos) > distanceMaxSqr)
                    {
                        index = optAgent.path.Count + 1;
                        break;
                    }
                    navMap.GetSquareXZ(pos, out var x, out var z);
                    if (NavUtils.TestMoveSquare(navMap, optAgent, x, z))
                    {
                        break;
                    }
                    index++;
                }
                if (index == optAgent.path.Count + 1)
                {
                    ReRequestPath(optAgent);
                }
                else
                {
                    index = optAgent.path.Count - index;
                    navQuery.InitSlicedFindPath(optAgent, optAgent.pos, optAgent.path[index], 0.0f);
                    var status = navQuery.UpdateSlicedFindPath(256, out _);
                    if ((status & NavQueryStatus.Success) == 0 || (status & NavQueryStatus.Partial) != 0)
                    {
                        ReRequestPath(optAgent);
                    }
                    else
                    {
                        navQuery.FinalizeSlicedFindPathWithSimpleSmooth(out var path);
                        ReplacePathStart(ref optAgent.path, index, path);
                    }
                }
            }
        }
        private static void UpdateDesiredVelocity(NavManager navManager, NavQuery[] navQueries, List<NavAgent> agents)
        {
            var navQuery = navQueries[0];
            var navMap = navManager.GetNavMap();
            foreach (var agent in agents)
            {
                var wantedDir = agent.flatFrontDir;
                var wantedSpeed = 0.0f;
                if (agent.moveState == NavMoveState.InProgress)
                {
                    Debug.Assert(agent.path != null && agent.path.Count > 0);

                    if (NavMathUtils.SqrDistance2D(agent.pos, agent.path[0]) <= agent.goalRadius * agent.goalRadius)
                    {
                        agent.moveState = NavMoveState.Idle;
                        if (NavMathUtils.SqrDistance2D(agent.pos, agent.goalPos) > agent.goalRadius * agent.goalRadius)
                        {
                            ReRequestPath(agent);
                        }
                    }
                    else
                    {
                        var wayPointDistSqr = NavMathUtils.Square(Mathf.Max(agent.param.maxSpeed * 1.05f, 1.25f * navMap.SquareSize));
                        var nextWayPoint = agent.path[agent.path.Count - 1];
                        while (agent.path.Count > 1 && NavMathUtils.SqrDistance2D(agent.pos, nextWayPoint) < wayPointDistSqr)
                        {
                            agent.path.RemoveAt(agent.path.Count - 1);
                            nextWayPoint = agent.path[agent.path.Count - 1];
                        }
                        wantedDir = NavMathUtils.Normalized2D(nextWayPoint - agent.pos);
                        wantedSpeed = agent.param.maxSpeed;
                    }
                }
                if (wantedDir != agent.flatFrontDir)
                {
                    agent.flatFrontDir = NavMathUtils.Rotate2D(agent.flatFrontDir, wantedDir, agent.param.maxTurnSpeed);
                }
                var deltaAngle = NavMathUtils.Angle2D(agent.flatFrontDir, wantedDir);
                if (deltaAngle > 0)
                {
                    wantedSpeed *= Mathf.Clamp(agent.param.maxTurnSpeed / deltaAngle, 0.1f, 1.0f);
                }
                var speedMod = NavUtils.GetSquareSpeedMod(navMap, agent, agent.pos, agent.flatFrontDir);
                wantedSpeed *= speedMod;
                var deltaSpeed = Mathf.Clamp(wantedSpeed - agent.speed, -agent.param.maxAcc, agent.param.maxAcc);
                agent.speed += deltaSpeed;
                agent.desiredVelocity = agent.flatFrontDir * agent.speed;
            }
        }
        private static void UpdateNewVelocity(NavManager navManager, NavQuery[] navQueries, List<NavAgent> agents)
        {
            var navMap = navManager.GetNavMap();
            foreach (var agent in agents)
            {
                agent.agentNeighbors.Clear();
                agent.obstacleNeighbors.Clear();

                float queryRadius = agent.radius + Mathf.Max(navMap.SquareSize, agent.param.maxSpeed) * 2.0f;
                navMap.GetSquareXZ(new Vector3(agent.pos.x - queryRadius, 0, agent.pos.z - queryRadius), out var sx, out var sz);
                navMap.GetSquareXZ(new Vector3(agent.pos.x + queryRadius, 0, agent.pos.z + queryRadius), out var ex, out var ez);
                for (int z = sz; z <= ez; z++)
                {
                    for (int x = sx; x <= ex; x++)
                    {
                        foreach (var other in navManager.GetSquareAgents(x, z))
                        {
                            if (other == agent)
                            {
                                continue;
                            }
                            if (!agent.agentNeighbors.Contains(other))
                            {
                                agent.agentNeighbors.Add(other);
                            }
                        }
                        if (!NavUtils.TestMoveSquareCenter(navMap, agent, x, z))
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
                NavRVO.ComputeNewVelocity(agent);
            }
        }
        private static void UpdatePos(NavManager navManager, NavQuery[] navQueries, List<NavAgent> agents)
        {
            var navQuery = navQueries[0];
            var navMap = navManager.GetNavMap();
            foreach (var agent in agents)
            {
                agent.lastPos = agent.pos;
                agent.pos = navMap.ClampInBounds(agent.pos + agent.newVelocity);
            }
        }
        private static void UpdateCollision(NavManager navManager, NavQuery[] navQueries, List<NavAgent> agents)
        {
            var navQuery = navQueries[0];
            var navMap = navManager.GetNavMap();
            foreach (var agent in agents)
            {
                // TODO
            }
        }
        private static void UpdateMove(NavManager navManager, NavQuery[] navQueries, List<NavAgent> agents)
        {
            var navQuery = navQueries[0];
            var navMap = navManager.GetNavMap();
            foreach (var agent in agents)
            {
                agent.isMoving = ((agent.pos - agent.lastPos).sqrMagnitude >= 1e-4f);
                // 更新索引
                var mapPos = NavUtils.CalcMapPos(navMap, agent.moveDef.GetUnitSize(), agent.pos);
                if (mapPos != agent.mapPos)
                {
                    navManager.RemoveSquareAgent(agent);
                    agent.mapPos = mapPos;
                    navManager.AddSquareAgent(agent);
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
        private static void ReRequestPath(NavAgent agent)
        {
            agent.isRepath = true;
        }
        private static void ReplacePathStart(ref List<Vector3> path, int index, List<Vector3> newStartPath)
        {
            path.RemoveRange(index, path.Count - index);
            path.AddRange(newStartPath);
        }
    }
}