using System.Collections.Generic;
using UnityEngine;

namespace GridNav
{
    public static class NavCrowdUpdate
    {

        public static void Update(NavManager navManager, NavQuery[] navQueries, List<NavAgent> agents)
        {
            UpdatePath(navManager, navQueries, agents); //多线程
            UpdateMoveRequest(navManager, navQueries, agents); // 单线程
            UpdatePrefVelocity(navManager, navQueries, agents); // 多线程
            UpdateNewVelocity(navManager, navQueries, agents); // 多线程
            UpdatePos(navManager, navQueries, agents); // 单线程
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
                agent.path.RemoveRange(agent.path.Count - checkStartIndex + 1, checkStartIndex - 1);
                var status = navQuery.InitSlicedFindPath(agent, agent.pos, agent.path[agent.path.Count - 1], navMap.SquareSize * 0.5f);
                navQuery.UpdateSlicedFindPath(128, out _);
                status = navQuery.FinalizeSlicedFindPath(out var path);
                if ((status & NavQueryStatus.Success) == 0 || (status & NavQueryStatus.Partial) != 0)
                {
                    ReRequestPath(agent);
                    continue;
                }
                for (int i = 1; i < path.Count; i++)
                {
                    agent.path.Add(path[i]);
                }
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
        private static void UpdatePrefVelocity(NavManager navManager, NavQuery[] navQueries, List<NavAgent> agents)
        {
            var navQuery = navQueries[0];
            var navMap = navManager.GetNavMap();
            foreach (var agent in agents)
            {
                if (agent.moveState != NavMoveState.InProgress)
                {
                    agent.prefVelocity = Vector3.zero;
                    continue;
                }
                Debug.Assert(agent.path != null && agent.path.Count > 0);
                if (NavMathUtils.SqrDistance2D(agent.pos, agent.path[0]) <= agent.goalRadius * agent.goalRadius)
                {
                    agent.prefVelocity = Vector3.zero;
                    agent.moveState = NavMoveState.Idle;
                    if (NavMathUtils.SqrDistance2D(agent.pos, agent.goalPos) > agent.goalRadius * agent.goalRadius)
                    {
                        ReRequestPath(agent);
                    }
                    continue;
                }
                var wayPointDistSqr = NavMathUtils.Square(Mathf.Max(agent.param.maxSpeed * 1.05f, 1.25f * navMap.SquareSize));
                var nextWayPoint = agent.path[agent.path.Count - 1];
                while (agent.path.Count > 1 && NavMathUtils.SqrDistance2D(agent.pos, nextWayPoint) < wayPointDistSqr)
                {
                    agent.path.RemoveAt(agent.path.Count - 1);
                    nextWayPoint = agent.path[agent.path.Count - 1];
                }
                agent.prefVelocity = NavMathUtils.Normalized2D(nextWayPoint - agent.pos) * agent.param.maxSpeed;
            }
        }
        private static void UpdateNewVelocity(NavManager navManager, NavQuery[] navQueries, List<NavAgent> agents)
        {
            var navMap = navManager.GetNavMap();
            var blockingObjectMap = navManager.GetBlockingObjectMap();
            foreach (var agent in agents)
            {
                CollectNeighbors(navMap, blockingObjectMap, agent);
                NavRVO.ComputeNewVelocity(agent, agent.obstacleNeighbors, agent.agentNeighbors);
            }
        }
        private static void UpdatePos(NavManager navManager, NavQuery[] navQueries, List<NavAgent> agents)
        {
            var navQuery = navQueries[0];
            var navMap = navManager.GetNavMap();
            var blockingObjectMap = navManager.GetBlockingObjectMap();
            foreach (var agent in agents)
            {
                var newPos = agent.pos + agent.newVelocity;
                newPos.y = navMap.GetHeight(newPos);
                navMap.ClampInBounds(newPos, out var x, out var z, out newPos);
                if (!NavUtils.TestMoveSquare(navMap, agent, x, z))
                {
                    NavUtils.ForeachNearestSquare(x, z, 20, (int tx, int tz) =>
                    {
                        if (tx < 0 || tx >= navMap.XSize || tz < 0 || tz >= navMap.ZSize)
                        {
                            return true;
                        }
                        if (NavUtils.TestMoveSquare(navMap, agent, tx, tz))
                        {
                            newPos = navMap.GetSquarePos(tx, tz);
                            //ReRequestPath(agent);
                            return false;
                        }
                        return true;
                    });
                }
                agent.lastPos = agent.pos;
                agent.pos = newPos;
                agent.velocity = newPos - agent.lastPos;
                agent.isMoving = (agent.velocity.sqrMagnitude >= 1e-4f);
                // 更新索引
                var mapPos = NavUtils.CalcMapPos(navMap, agent.moveDef.GetUnitSize(), agent.pos);
                if (mapPos != agent.mapPos)
                {
                    blockingObjectMap.RemoveAgent(agent);
                    agent.mapPos = mapPos;
                    blockingObjectMap.AddAgent(agent);
                }
            }
        }
        private static void CollectNeighbors(NavMap navMap, NavBlockingObjectMap blockingObjectMap, NavAgent agent)
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
                    foreach (var other in blockingObjectMap.GetSquareAgents(x, z))
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
    }
}