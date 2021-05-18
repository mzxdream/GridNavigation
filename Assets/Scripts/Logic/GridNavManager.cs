using System.Collections.Generic;
using UnityEngine;

public class GridNavManager
{
    private GridNavMesh navMesh;
    private GridNavQuery navQuery;
    private int lastAgentID;
    private Dictionary<int, GridNavAgent> agents;
    private Dictionary<int, List<GridNavAgent>> squareAgents;
    private GridNavQuery pathRequestNavQuery;
    private List<int> pathRequestQueue;
    private int tempNum;

    public bool Init(GridNavMesh navMesh, int maxAgents = 1024)
    {
        Debug.Assert(navMesh != null && maxAgents > 0);
        this.navMesh = navMesh;
        this.navQuery = new GridNavQuery();
        if (!navQuery.Init(navMesh))
        {
            return false;
        }
        this.lastAgentID = 0;
        this.agents = new Dictionary<int, GridNavAgent>();
        this.squareAgents = new Dictionary<int, List<GridNavAgent>>();
        this.pathRequestNavQuery = new GridNavQuery();
        if (!this.pathRequestNavQuery.Init(navMesh))
        {
            return false;
        }
        this.pathRequestQueue = new List<int>();
        this.tempNum = 0;
        return true;
    }
    public void Clear()
    {
        this.navQuery.Clear();
        this.pathRequestNavQuery.Clear();
    }
    public int AddAgent(Vector3 pos, Vector3 forward, GridNavAgentParam param)
    {
        int unitSize = (int)(param.radius * 2 / navMesh.SquareSize - 1e-4f) + 1;
        if ((unitSize & 1) == 0)
        {
            unitSize++;
        }
        var agent = new GridNavAgent
        {
            id = ++lastAgentID,
            param = param,
            pos = pos,
            frontDir = new Vector3(forward.x, 0, forward.z).normalized,
            unitSize = unitSize,
            maxInteriorRadius = unitSize * navMesh.SquareSize / 2.0f - 1e-4f,
            state = GridNavAgentState.None,
            squareIndex = 0,
            goalPos = Vector3.zero,
            goalRadius = 0.0f,
            goalSquareIndex = 0,
            prefVelocity = Vector3.zero,
            velocity = Vector3.zero,
            newVelocity = Vector3.zero,
            tempNum = 0,
        };
        agent.filter = new GridNavQueryFilterExtraBlockedCheck(unitSize, (int index) =>
        {
            if (squareAgents.TryGetValue(index, out var squareAgentList))
            {
                foreach (var squareAgent in squareAgentList)
                {
                    if (squareAgent != agent)
                    {
                        return true;
                    }
                }
            }
            return false;
        });
        agent.pathFilter = new GridNavQueryFilterExtraBlockedCheck(unitSize, (int index) =>
        {
            if (squareAgents.TryGetValue(index, out var squareAgentList))
            {
                foreach (var squareAgent in squareAgentList)
                {
                    if (squareAgent != agent && squareAgent.velocity.sqrMagnitude < 0.00001f)
                    {
                        return true;
                    }
                }
            }
            return false;
        });
        if (navQuery.FindNearestSquare(agent.filter, agent.pos, agent.param.radius * 20.0f, out var nearestIndex, out var nearesetPos))
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
            if (agent.state == GridNavAgentState.Requesting)
            {
                pathRequestQueue.Remove(agent.id);
            }
            else if (agent.state == GridNavAgentState.WaitForPath)
            {
                Debug.Assert(pathRequestQueue[0] == agent.id);
                pathRequestQueue.RemoveAt(0);
            }
            RemoveSquareAgent(agent.squareIndex, agent);
            agents.Remove(agentID);
        }
    }
    public void Update(float deltaTime)
    {
        foreach (var a in agents) //移到合法点
        {
            var agent = a.Value;
            if (agent.filter.IsBlocked(navMesh, agent.squareIndex))
            {
                if (navQuery.FindNearestSquare(agent.filter, agent.pos, agent.param.radius * 20.0f, out var nearestIndex, out var nearesetPos))
                {
                    RemoveSquareAgent(agent.squareIndex, agent);
                    AddSquareAgent(nearestIndex, agent);
                    agent.squareIndex = nearestIndex;
                    agent.pos = nearesetPos;
                    if (agent.state == GridNavAgentState.WaitForPath)
                    {
                        Debug.Assert(pathRequestQueue[0] == agent.id);
                        pathRequestQueue.RemoveAt(0);
                        pathRequestQueue.Add(agent.id);
                    }
                }
                else
                {
                    if (agent.state == GridNavAgentState.WaitForPath)
                    {
                        Debug.Assert(pathRequestQueue[0] == agent.id);
                        pathRequestQueue.RemoveAt(0);
                    }
                    else if (agent.state == GridNavAgentState.Requesting)
                    {
                        pathRequestQueue.Remove(agent.id);
                    }
                    agent.state = GridNavAgentState.None;
                }
            }
        }
        int maxNodes = 10240;
        while (pathRequestQueue.Count > 0 && maxNodes > 0) //寻路
        {
            var agent = agents[pathRequestQueue[0]];
            if (agent.state == GridNavAgentState.Requesting)
            {
                agent.state = GridNavAgentState.WaitForPath;
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
        foreach (var a in agents) //更改方向和移动速度
        {
            var agent = a.Value;
            agent.pos.y = 0.0f;
            if (agent.state != GridNavAgentState.Moving)
            {
                agent.prefVelocity = Vector3.zero;
                continue;
            }
            Debug.Assert(agent.path.Count > 0);
            var pathSquareIndex = agent.path[agent.path.Count - 1];
            var goalPos = pathSquareIndex == agent.goalSquareIndex ? agent.goalPos : navMesh.GetSquarePos(pathSquareIndex);
            if (GridNavMath.SqrDistance2D(agent.pos, goalPos) <= agent.goalRadius * agent.goalRadius)
            {
                if (pathSquareIndex == agent.goalSquareIndex)
                {
                    agent.prefVelocity = Vector3.zero;
                    agent.state = GridNavAgentState.None;
                }
                else
                {
                    StartMoving(agent, agent.goalPos, agent.goalRadius);
                }
                continue;
            }
            while (agent.path.Count > 1 && navMesh.DistanceApproximately(agent.squareIndex, agent.path[0]) <= 10.0f * navMesh.SquareSize)
            {
                agent.path.RemoveAt(0);
            }
            bool foundNextWayPoint = false;
            while (agent.path.Count > 0 && navMesh.DistanceApproximately(agent.squareIndex, agent.path[0]) <= 15.0f * navMesh.SquareSize)
            {
                if (!agent.pathFilter.IsBlocked(navMesh, agent.path[0]))
                {
                    foundNextWayPoint = true;
                    break;
                }
                agent.path.RemoveAt(0);
            }
            if (!foundNextWayPoint)
            {
                agent.prefVelocity = Vector3.zero;
                StartMoving(agent, agent.goalPos, agent.goalRadius);
                continue;
            }
            agent.path2 = null;
            var nextSquareIndex = agent.squareIndex;
            if (agent.path[0] != agent.squareIndex)
            {
                if (!navQuery.Raycast(agent.pathFilter, agent.squareIndex, agent.path[0], out var path, out var totalCost))
                {
                    var constraint = new GridNavQueryConstraintCircleStrict(agent.path[0], agent.squareIndex, 16.0f * navMesh.SquareSize);
                    if (!navQuery.FindPath(agent.pathFilter, agent.squareIndex, constraint, out path) || path[path.Count - 1] != agent.path[0])
                    {
                        agent.prefVelocity = Vector3.zero;
                        StartMoving(agent, agent.goalPos, agent.goalRadius);
                        continue;
                    }
                    navQuery.FindStraightPath(agent.pathFilter, path, out var straightPath);
                    nextSquareIndex = path[1];
                }
                else
                {
                    nextSquareIndex = path[path.Count - 1];
                }
                agent.path2 = path;
            }
            var nextPos = nextSquareIndex == agent.goalSquareIndex ? agent.goalPos : navMesh.GetSquarePos(nextSquareIndex);
            var disiredDir = GridNavMath.Normalized2D(nextPos - agent.pos);
            agent.prefVelocity = disiredDir * agent.param.maxSpeed;
        }
        foreach (var a in agents)
        {
            var agent = a.Value;
            if (agent.state != GridNavAgentState.Moving)
            {
                agent.newVelocity = Vector3.zero;
                continue;
            }
            CollectNeighbors(agent);
            ComputeNewVelocity(agent, deltaTime);
        }
        //更新坐标
        foreach (var a in agents)
        {
            var agent = a.Value;
            agent.velocity = agent.newVelocity;
            if (agent.velocity.y > 0.0f)
            {
                var t = 1;
                t++;
            }
            if (agent.velocity.sqrMagnitude > 0.0001f)
            {
                agent.frontDir = agent.velocity.normalized;
                if (agent.frontDir.y > 0.0f)
                {
                    int t = 1;
                    t++;
                }
            }
            else
            {
                agent.velocity = Vector3.zero;
                continue;
            }
            navMesh.ClampInBounds(agent.pos + agent.velocity * deltaTime, out var nextSquareIndex, out var nextPos);
            if (agent.filter.IsBlocked(navMesh, nextSquareIndex))
            {
                if (navQuery.FindNearestSquare(agent.filter, agent.pos, agent.param.radius * 20.0f, out var nearestIndex, out var nearesetPos))
                {
                    nextPos = nearesetPos;
                }
                else
                {
                    nextPos = agent.pos;
                }
            }
            //if (!navQuery.Raycast(agent.filter, agent.squareIndex, nextSquareIndex, out var path, out var totalCost))
            //{
            //    if (path.Count > 0 && path[path.Count - 1] != agent.squareIndex)
            //    {
            //        nextPos = navMesh.GetSquarePos(path[path.Count - 1]);
            //    }
            //    else
            //    {
            //        nextPos = agent.pos;
            //    }
            //}
            if (GridNavMath.SqrDistance2D(agent.pos, nextPos) <= 1e-4f)
            {
                agent.velocity = Vector3.zero;
            }
            agent.pos = nextPos;
            var newSquareIndex = navMesh.GetSquareIndex(agent.pos);
            if (newSquareIndex != agent.squareIndex)
            {
                RemoveSquareAgent(agent.squareIndex, agent);
                AddSquareAgent(newSquareIndex, agent);
                agent.squareIndex = newSquareIndex;
            }
        }

    }
    private void ComputeNewVelocity(GridNavAgent agent, float deltaTime)
    {
        var orcaLines = new List<Line>();

        float timeHorizonObst = 1.0f;
        float invTimeHorizonObst = 1.0f / timeHorizonObst;

        float radius = agent.maxInteriorRadius;
        /* Create obstacle ORCA lines. */
        for (int i = 0; i < agent.obstacleNeighbors.Count; ++i)
        {
            Obstacle obstacle1 = agent.obstacleNeighbors[i];
            Obstacle obstacle2 = obstacle1.next_;

            Vector3 relativePosition1 = obstacle1.point_ - agent.pos;
            Vector3 relativePosition2 = obstacle2.point_ - agent.pos;

            bool alreadyCovered = false;

            for (int j = 0; j < orcaLines.Count; ++j)
            {
                if (GridNavMath.Det2D(invTimeHorizonObst * relativePosition1 - orcaLines[j].point, orcaLines[j].direction) - invTimeHorizonObst * agent.maxInteriorRadius >= -1e-5f
                     && GridNavMath.Det2D(invTimeHorizonObst * relativePosition2 - orcaLines[j].point, orcaLines[j].direction) - invTimeHorizonObst * agent.maxInteriorRadius >= -1e-5f)
                {
                    alreadyCovered = true;

                    break;
                }
            }
            if (alreadyCovered)
            {
                continue;
            }
            /* Not yet covered. Check for collisions. */
            float distSq1 = GridNavMath.SqrMagnitude2D(relativePosition1);
            float distSq2 = GridNavMath.SqrMagnitude2D(relativePosition2);

            float radiusSq = agent.maxInteriorRadius * agent.maxInteriorRadius;

            Vector3 obstacleVector = obstacle2.point_ - obstacle1.point_;
            float s = GridNavMath.Dot2D(-relativePosition1, obstacleVector) / GridNavMath.SqrMagnitude2D(obstacleVector);
            float distSqLine = GridNavMath.SqrMagnitude2D(-relativePosition1 - s * obstacleVector);

            Line line;

            if (s < 0.0f && distSq1 <= radiusSq)
            {
                /* Collision with left vertex. Ignore if non-convex. */
                if (obstacle1.convex_)
                {
                    line.point = new Vector3(0.0f, 0.0f, 0.0f);
                    line.direction = GridNavMath.Normalized2D(new Vector3(-relativePosition1.z, 0, relativePosition1.x));
                    orcaLines.Add(line);
                }

                continue;
            }
            else if (s > 1.0f && distSq2 <= radiusSq)
            {
                /*
                 * Collision with right vertex. Ignore if non-convex or if
                 * it will be taken care of by neighboring obstacle.
                 */
                if (obstacle2.convex_ && GridNavMath.Det2D(relativePosition2, obstacle2.direction_) >= 0.0f)
                {
                    line.point = new Vector3(0.0f, 0.0f, 0.0f);
                    line.direction = GridNavMath.Normalized2D(new Vector3(-relativePosition2.z, 0, relativePosition2.x));
                    orcaLines.Add(line);
                }

                continue;
            }
            else if (s >= 0.0f && s < 1.0f && distSqLine <= radiusSq)
            {
                /* Collision with obstacle segment. */
                line.point = new Vector3(0.0f, 0.0f, 0.0f);
                line.direction = -obstacle1.direction_;
                orcaLines.Add(line);

                continue;
            }

            /*
             * No collision. Compute legs. When obliquely viewed, both legs
             * can come from a single vertex. Legs extend cut-off line when
             * non-convex vertex.
             */

            Vector3 leftLegDirection, rightLegDirection;

            if (s < 0.0f && distSqLine <= radiusSq)
            {
                /*
                 * Obstacle viewed obliquely so that left vertex
                 * defines velocity obstacle.
                 */
                if (!obstacle1.convex_)
                {
                    /* Ignore obstacle. */
                    continue;
                }

                obstacle2 = obstacle1;

                float leg1 = Mathf.Sqrt(distSq1 - radiusSq);
                leftLegDirection = new Vector3(relativePosition1.x * leg1 - relativePosition1.z * radius, 0, relativePosition1.x * radius + relativePosition1.z * leg1) / distSq1;
                rightLegDirection = new Vector3(relativePosition1.x * leg1 + relativePosition1.z * radius, 0, -relativePosition1.x * radius + relativePosition1.z * leg1) / distSq1;
            }
            else if (s > 1.0f && distSqLine <= radiusSq)
            {
                /*
                 * Obstacle viewed obliquely so that
                 * right vertex defines velocity obstacle.
                 */
                if (!obstacle2.convex_)
                {
                    /* Ignore obstacle. */
                    continue;
                }

                obstacle1 = obstacle2;

                float leg2 = Mathf.Sqrt(distSq2 - radiusSq);
                leftLegDirection = new Vector3(relativePosition2.x * leg2 - relativePosition2.z * radius, 0, relativePosition2.x * radius + relativePosition2.z * leg2) / distSq2;
                rightLegDirection = new Vector3(relativePosition2.x * leg2 + relativePosition2.z * radius, 0, -relativePosition2.x * radius + relativePosition2.z * leg2) / distSq2;
            }
            else
            {
                /* Usual situation. */
                if (obstacle1.convex_)
                {
                    float leg1 = Mathf.Sqrt(distSq1 - radiusSq);
                    leftLegDirection = new Vector3(relativePosition1.x * leg1 - relativePosition1.z * radius, 0, relativePosition1.x * radius + relativePosition1.z * leg1) / distSq1;
                }
                else
                {
                    /* Left vertex non-convex; left leg extends cut-off line. */
                    leftLegDirection = -obstacle1.direction_;
                }

                if (obstacle2.convex_)
                {
                    float leg2 = Mathf.Sqrt(distSq2 - radiusSq);
                    rightLegDirection = new Vector3(relativePosition2.x * leg2 + relativePosition2.z * radius, 0, -relativePosition2.x * radius + relativePosition2.z * leg2) / distSq2;
                }
                else
                {
                    /* Right vertex non-convex; right leg extends cut-off line. */
                    rightLegDirection = obstacle1.direction_;
                }
            }

            /*
             * Legs can never point into neighboring edge when convex
             * vertex, take cutoff-line of neighboring edge instead. If
             * velocity projected on "foreign" leg, no constraint is added.
             */

            Obstacle leftNeighbor = obstacle1.previous_;

            bool isLeftLegForeign = false;
            bool isRightLegForeign = false;

            if (obstacle1.convex_ && GridNavMath.Det2D(leftLegDirection, -leftNeighbor.direction_) >= 0.0f)
            {
                /* Left leg points into obstacle. */
                leftLegDirection = -leftNeighbor.direction_;
                isLeftLegForeign = true;
            }

            if (obstacle2.convex_ && GridNavMath.Det2D(rightLegDirection, obstacle2.direction_) <= 0.0f)
            {
                /* Right leg points into obstacle. */
                rightLegDirection = obstacle2.direction_;
                isRightLegForeign = true;
            }

            /* Compute cut-off centers. */
            Vector3 leftCutOff = invTimeHorizonObst * (obstacle1.point_ - agent.pos);
            Vector3 rightCutOff = invTimeHorizonObst * (obstacle2.point_ - agent.pos);
            Vector3 cutOffVector = rightCutOff - leftCutOff;

            /* Project current velocity on velocity obstacle. */

            /* Check if current velocity is projected on cutoff circles. */
            float t = obstacle1 == obstacle2 ? 0.5f : GridNavMath.Dot2D(agent.velocity - leftCutOff, cutOffVector) / GridNavMath.SqrMagnitude2D(cutOffVector);
            float tLeft = GridNavMath.Dot2D(agent.velocity - leftCutOff, leftLegDirection);
            float tRight = GridNavMath.Dot2D(agent.velocity - rightCutOff, rightLegDirection);

            if ((t < 0.0f && tLeft < 0.0f) || (obstacle1 == obstacle2 && tLeft < 0.0f && tRight < 0.0f))
            {
                /* Project on left cut-off circle. */
                Vector3 unitW = GridNavMath.Normalized2D(agent.velocity - leftCutOff);

                line.direction = new Vector3(unitW.z, 0, -unitW.x);
                line.point = leftCutOff + radius * invTimeHorizonObst * unitW;
                orcaLines.Add(line);

                continue;
            }
            else if (t > 1.0f && tRight < 0.0f)
            {
                /* Project on right cut-off circle. */
                Vector3 unitW = GridNavMath.Normalized2D(agent.velocity - rightCutOff);

                line.direction = new Vector3(unitW.z, 0, -unitW.x);
                line.point = rightCutOff + radius * invTimeHorizonObst * unitW;
                orcaLines.Add(line);

                continue;
            }

            /*
             * Project on left leg, right leg, or cut-off line, whichever is
             * closest to velocity.
             */
            float distSqCutoff = (t < 0.0f || t > 1.0f || obstacle1 == obstacle2) ? float.PositiveInfinity : GridNavMath.SqrMagnitude2D(agent.velocity - (leftCutOff + t * cutOffVector));
            float distSqLeft = tLeft < 0.0f ? float.PositiveInfinity : GridNavMath.SqrMagnitude2D(agent.velocity - (leftCutOff + tLeft * leftLegDirection));
            float distSqRight = tRight < 0.0f ? float.PositiveInfinity : GridNavMath.SqrMagnitude2D(agent.velocity - (rightCutOff + tRight * rightLegDirection));

            if (distSqCutoff <= distSqLeft && distSqCutoff <= distSqRight)
            {
                /* Project on cut-off line. */
                line.direction = -obstacle1.direction_;
                line.point = leftCutOff + radius * invTimeHorizonObst * new Vector3(-line.direction.z, 0, line.direction.x);
                orcaLines.Add(line);

                continue;
            }

            if (distSqLeft <= distSqRight)
            {
                /* Project on left leg. */
                if (isLeftLegForeign)
                {
                    continue;
                }

                line.direction = leftLegDirection;
                line.point = leftCutOff + radius * invTimeHorizonObst * new Vector3(-line.direction.z, 0, line.direction.x);
                orcaLines.Add(line);

                continue;
            }

            /* Project on right leg. */
            if (isRightLegForeign)
            {
                continue;
            }

            line.direction = -rightLegDirection;
            line.point = rightCutOff + radius * invTimeHorizonObst * new Vector3(-line.direction.z, 0, line.direction.x);
            orcaLines.Add(line);
        }


        //obstacle
        int numObstLines = orcaLines.Count;

        var timeHorizon = 1.0f;
        float invTimeHorizon = 1.0f / timeHorizon;

        /* Create agent ORCA lines. */
        for (int i = 0; i < agent.neighbors.Count; ++i)
        {
            var other = agent.neighbors[i];
            Vector3 relativePosition = other.pos - agent.pos;
            // mass
            float massRatio = (other.param.mass / (agent.param.mass + other.param.mass));
            float neighborMassRatio = (agent.param.mass / (agent.param.mass + other.param.mass));
            Vector3 velocityOpt = (massRatio >= 0.5f ? (agent.velocity - massRatio * agent.velocity) * 2 : agent.prefVelocity + (agent.velocity - agent.prefVelocity) * massRatio * 2);
            Vector3 neighborVelocityOpt = (neighborMassRatio >= 0.5f ? 2 * other.velocity * (1 - neighborMassRatio) : other.prefVelocity + (other.velocity - other.prefVelocity) * neighborMassRatio * 2);

            Vector3 relativeVelocity = velocityOpt - neighborVelocityOpt;
            float distSq = GridNavMath.SqrMagnitude2D(relativePosition);
            float combinedRadius = agent.maxInteriorRadius + other.maxInteriorRadius;
            float combinedRadiusSq = combinedRadius * combinedRadius;

            Line line;
            Vector3 u;

            if (distSq > combinedRadiusSq)
            {
                /* No collision. */
                Vector3 w = relativeVelocity - invTimeHorizon * relativePosition;

                /* Vector from cutoff center to relative velocity. */
                float wLengthSq = GridNavMath.SqrMagnitude2D(w);
                float dotProduct1 = GridNavMath.Dot2D(w, relativePosition);

                if (dotProduct1 < 0.0f && dotProduct1 * dotProduct1 > combinedRadiusSq * wLengthSq)
                {
                    /* Project on cut-off circle. */
                    float wLength = Mathf.Sqrt(wLengthSq);
                    Vector3 unitW = w / wLength;

                    line.direction = new Vector3(unitW.z, 0.0f, -unitW.x);
                    u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                }
                else
                {
                    /* Project on legs. */
                    float leg = Mathf.Sqrt(distSq - combinedRadiusSq);

                    if (GridNavMath.Det2D(relativePosition, w) > 0.0f)
                    {
                        /* Project on left leg. */
                        line.direction = new Vector3(relativePosition.x * leg - relativePosition.z * combinedRadius, 0.0f, relativePosition.x * combinedRadius + relativePosition.z * leg) / distSq;
                    }
                    else
                    {
                        /* Project on right leg. */
                        line.direction = -new Vector3(relativePosition.x * leg + relativePosition.z * combinedRadius, 0.0f, -relativePosition.x * combinedRadius + relativePosition.z * leg) / distSq;
                    }

                    float dotProduct2 = GridNavMath.Dot2D(relativeVelocity, line.direction);
                    u = dotProduct2 * line.direction - relativeVelocity;
                }
            }
            else
            {
                /* Collision. Project on cut-off circle of time timeStep. */
                float invTimeStep = 1.0f / deltaTime;

                /* Vector from cutoff center to relative velocity. */
                Vector3 w = relativeVelocity - invTimeStep * relativePosition;

                float wLength = GridNavMath.Magnitude2D(w);
                Vector3 unitW = w / wLength;

                line.direction = new Vector3(unitW.z, 0, -unitW.x);
                u = (combinedRadius * invTimeStep - wLength) * unitW;
            }

            //line.point = velocityOpt + 0.5f * u;
            line.point = velocityOpt + massRatio * u;
            orcaLines.Add(line);
        }
        bool msDirectionOpt = false;
        int lineFail = linearProgram2(orcaLines, agent.param.maxSpeed, agent.prefVelocity, msDirectionOpt, ref agent.newVelocity);
        if (lineFail < orcaLines.Count)
        {
            linearProgram3(orcaLines, numObstLines, lineFail, agent.param.maxSpeed, ref agent.newVelocity);
        }
    }
    private void CollectNeighbors(GridNavAgent agent)
    {
        agent.neighbors.Clear();
        agent.obstacleNeighbors.Clear();
        tempNum++;
        float radius = 10f * navMesh.SquareSize;
        navMesh.GetSquareXZ(new Vector3(agent.pos.x - radius, 0, agent.pos.z - radius), out var sx, out var sz);
        navMesh.GetSquareXZ(new Vector3(agent.pos.x + radius, 0, agent.pos.z + radius), out var ex, out var ez);
        for (int z = sz; z <= ez; z++)
        {
            for (int x = sx; x <= ex; x++)
            {
                var index = navMesh.GetSquareIndex(x, z);
                var isBlocked = false;
                if (squareAgents.TryGetValue(index, out var agentList))
                {
                    foreach (var other in agentList)
                    {
                        if (other.tempNum == this.tempNum || other == agent)
                        {
                            continue;
                        }
                        other.tempNum = this.tempNum;
                        agent.neighbors.Add(other);
                        if (other.velocity.sqrMagnitude <= 0.00001f)
                        {
                            isBlocked = true;
                        }
                    }
                }
                if (isBlocked || navMesh.IsSquareBlocked(index))
                {
                    List<Vector3> vertices = new List<Vector3>();

                    var point = navMesh.GetSquarePos(index);
                    vertices.Add(new Vector3(point.x + navMesh.SquareSize / 2, 0, point.z + navMesh.SquareSize / 2));
                    vertices.Add(new Vector3(point.x - navMesh.SquareSize / 2, 0, point.z + navMesh.SquareSize / 2));
                    vertices.Add(new Vector3(point.x - navMesh.SquareSize / 2, 0, point.z - navMesh.SquareSize / 2));
                    vertices.Add(new Vector3(point.x + navMesh.SquareSize / 2, 0, point.z - navMesh.SquareSize / 2));
                    AddObstacle(vertices, ref agent.obstacleNeighbors);
                }
            }
        }
    }

    public void AddObstacle(IList<Vector3> vertices, ref List<Obstacle> obstacles)
    {
        if (vertices.Count < 2)
        {
            return;
        }
        int obstacleNo = obstacles.Count;
        for (int i = 0; i < vertices.Count; ++i)
        {
            Obstacle obstacle = new Obstacle();
            obstacle.point_ = vertices[i];

            if (i != 0)
            {
                obstacle.previous_ = obstacles[obstacles.Count - 1];
                obstacle.previous_.next_ = obstacle;
            }

            if (i == vertices.Count - 1)
            {
                obstacle.next_ = obstacles[obstacleNo];
                obstacle.next_.previous_ = obstacle;
            }

            obstacle.direction_ = GridNavMath.Normalized2D(vertices[(i == vertices.Count - 1 ? 0 : i + 1)] - vertices[i]);

            if (vertices.Count == 2)
            {
                obstacle.convex_ = true;
            }
            else
            {
                obstacle.convex_ = (GridNavMath.LeftOf2D(vertices[(i == 0 ? vertices.Count - 1 : i - 1)], vertices[i], vertices[(i == vertices.Count - 1 ? 0 : i + 1)]) >= 0.0f);
            }
            obstacles.Add(obstacle);
        }
    }

    public bool StartMoving(int agentID, Vector3 goalPos)
    {
        if (!agents.TryGetValue(agentID, out var agent))
        {
            return false;
        }
        StartMoving(agent, goalPos, 0.1f);
        return true;
    }
    private void StartMoving(GridNavAgent agent, Vector3 goalPos, float goalRadius)
    {
        navMesh.ClampInBounds(goalPos, out var neareastIndex, out var nearestPos);
        if (agent.state == GridNavAgentState.Requesting)
        {
            agent.goalPos = nearestPos;
            agent.goalRadius = goalRadius;
            return;
        }
        if (agent.state == GridNavAgentState.WaitForPath)
        {
            Debug.Assert(agent.id == pathRequestQueue[0]);
            if (neareastIndex == agent.goalSquareIndex && Mathf.Abs(goalRadius - agent.goalRadius) < navMesh.SquareSize)
            {
                return;
            }
            pathRequestQueue.RemoveAt(0);
        }
        agent.state = GridNavAgentState.Requesting;
        agent.goalPos = nearestPos;
        agent.goalRadius = goalRadius;
        agent.goalSquareIndex = neareastIndex;
        agent.prefVelocity = Vector3.zero;
        pathRequestQueue.Add(agent.id);
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
        forward = agent.frontDir;
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
    private void AddSquareAgent(int index, GridNavAgent agent)
    {
        int halfUnitSize = agent.unitSize >> 1;
        navMesh.GetSquareXZ(index, out var x, out var z);
        int xmin = Mathf.Max(0, x - halfUnitSize);
        int zmin = Mathf.Max(0, z - halfUnitSize);
        int xmax = Mathf.Min(navMesh.XSize - 1, x + halfUnitSize);
        int zmax = Mathf.Min(navMesh.ZSize - 1, z + halfUnitSize);
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
        int halfUnitSize = agent.unitSize >> 1;
        navMesh.GetSquareXZ(index, out var x, out var z);
        int xmin = Mathf.Max(0, x - halfUnitSize);
        int zmin = Mathf.Max(0, z - halfUnitSize);
        int xmax = Mathf.Min(navMesh.XSize - 1, x + halfUnitSize);
        int zmax = Mathf.Min(navMesh.ZSize - 1, z + halfUnitSize);
        for (int tz = zmin; tz <= zmax; tz++)
        {
            for (int tx = xmin; tx <= xmax; tx++)
            {
                index = navMesh.GetSquareIndex(tx, tz);
                if (squareAgents.TryGetValue(index, out var agentList))
                {
                    agentList.Remove(agent);
                    if (agentList.Count == 0)
                    {
                        squareAgents.Remove(index);
                    }
                }
            }
        }
    }

    private static bool linearProgram1(IList<Line> lines, int lineNo, float radius, Vector3 optVelocity, bool directionOpt, ref Vector3 result)
    {
        float dotProduct = GridNavMath.Dot2D(lines[lineNo].point, lines[lineNo].direction);
        float discriminant = dotProduct * dotProduct + radius * radius - GridNavMath.SqrMagnitude2D(lines[lineNo].point);

        if (discriminant < 0.0f)
        {
            /* Max speed circle fully invalidates line lineNo. */
            return false;
        }

        float sqrtDiscriminant = Mathf.Sqrt(discriminant);
        float tLeft = -dotProduct - sqrtDiscriminant;
        float tRight = -dotProduct + sqrtDiscriminant;

        for (int i = 0; i < lineNo; ++i)
        {
            float denominator = GridNavMath.Det2D(lines[lineNo].direction, lines[i].direction);
            float numerator = GridNavMath.Det2D(lines[i].direction, lines[lineNo].point - lines[i].point);

            if (Mathf.Abs(denominator) <= 0.00001f)
            {
                /* Lines lineNo and i are (almost) parallel. */
                if (numerator < 0.0f)
                {
                    return false;
                }

                continue;
            }

            float t = numerator / denominator;

            if (denominator >= 0.0f)
            {
                /* Line i bounds line lineNo on the right. */
                tRight = Mathf.Min(tRight, t);
            }
            else
            {
                /* Line i bounds line lineNo on the left. */
                tLeft = Mathf.Max(tLeft, t);
            }

            if (tLeft > tRight)
            {
                return false;
            }
        }

        if (directionOpt)
        {
            /* Optimize direction. */
            if (GridNavMath.Dot2D(optVelocity, lines[lineNo].direction) > 0.0f)
            {
                /* Take right extreme. */
                result = lines[lineNo].point + tRight * lines[lineNo].direction;
            }
            else
            {
                /* Take left extreme. */
                result = lines[lineNo].point + tLeft * lines[lineNo].direction;
            }
        }
        else
        {
            /* Optimize closest point. */
            float t = GridNavMath.Dot2D(lines[lineNo].direction, (optVelocity - lines[lineNo].point));

            if (t < tLeft)
            {
                result = lines[lineNo].point + tLeft * lines[lineNo].direction;
            }
            else if (t > tRight)
            {
                result = lines[lineNo].point + tRight * lines[lineNo].direction;
            }
            else
            {
                result = lines[lineNo].point + t * lines[lineNo].direction;
            }
        }

        return true;
    }

    private int linearProgram2(IList<Line> lines, float radius, Vector3 optVelocity, bool directionOpt, ref Vector3 result)
    {
        // directionOpt 第一次为false，第二次为true，directionOpt主要用在 linearProgram1 里面
        if (directionOpt)
        {
            /*
             * Optimize direction. Note that the optimization velocity is of
             * unit length in this case.
             */
            // 1.这个其实没什么用，只是因为velocity是归一化的所以直接乘 radius
            result = optVelocity * radius;
        }
        else if (GridNavMath.SqrMagnitude2D(optVelocity) > radius * radius)
        {
            /* Optimize closest point and outside circle. */
            // 2.当 optVelocity 太大时，先归一化optVelocity，再乘 radius
            result = GridNavMath.Normalized2D(optVelocity) * radius;
        }
        else
        {
            /* Optimize closest point and inside circle. */
            // 3.当 optVelocity 小于maxSpeed时
            result = optVelocity;
        }

        for (int i = 0; i < lines.Count; ++i)
        {
            if (GridNavMath.Det2D(lines[i].direction, lines[i].point - result) > 0.0f)
            {
                /* Result does not satisfy constraint i. Compute new optimal result. */
                Vector3 tempResult = result;
                if (!linearProgram1(lines, i, radius, optVelocity, directionOpt, ref result))
                {
                    result = tempResult;

                    return i;
                }
            }
        }

        return lines.Count;
    }

    private void linearProgram3(IList<Line> lines, int numObstLines, int beginLine, float radius, ref Vector3 result)
    {

        float distance = 0.0f;
        // 遍历所有剩余ORCA线
        for (int i = beginLine; i < lines.Count; ++i)
        {
            // 每一条 ORCA 线都需要精确的做出处理，distance 为 最大违规的速度
            if (GridNavMath.Det2D(lines[i].direction, lines[i].point - result) > distance)
            {
                /* Result does not satisfy constraint of line i. */
                IList<Line> projLines = new List<Line>();
                // 1.静态阻挡的orca线直接加到projLines中
                for (int ii = 0; ii < numObstLines; ++ii)
                {
                    projLines.Add(lines[ii]);
                }
                // 2.动态阻挡的orca线需要重新计算line，从第一个非静态阻挡到当前的orca线
                for (int j = numObstLines; j < i; ++j)
                {
                    Line line;

                    float determinant = GridNavMath.Det2D(lines[i].direction, lines[j].direction);

                    if (Mathf.Abs(determinant) <= 0.00001f)
                    {
                        /* Line i and line j are parallel. */
                        if (GridNavMath.Dot2D(lines[i].direction, lines[j].direction) > 0.0f)
                        {
                            /* Line i and line j point in the same direction. */
                            // 2-1 两条线平行且同向
                            continue;
                        }
                        else
                        {
                            /* Line i and line j point in opposite direction. */
                            // 2-2 两条线平行且反向
                            line.point = 0.5f * (lines[i].point + lines[j].point);
                        }
                    }
                    else
                    {
                        // 2-3 两条线不平行
                        line.point = lines[i].point + (GridNavMath.Det2D(lines[j].direction, lines[i].point - lines[j].point) / determinant) * lines[i].direction;
                    }
                    // 计算ORCA线的方向
                    line.direction = GridNavMath.Normalized2D(lines[j].direction - lines[i].direction);
                    projLines.Add(line);
                }
                // 3.再次计算最优速度
                Vector2 tempResult = result;
                // 注意这里的 new Vector2(-lines[i].direction.y(), lines[i].direction.x()) 是方向向量
                if (linearProgram2(projLines, radius, new Vector3(-lines[i].direction.z, 0.0f, lines[i].direction.x), true, ref result) < projLines.Count)
                {
                    /*
                     * This should in principle not happen. The result is by
                     * definition already in the feasible region of this
                     * linear program. If it fails, it is due to small
                     * floating point error, and the current result is kept.
                     */
                    result = tempResult;
                }

                distance = GridNavMath.Det2D(lines[i].direction, lines[i].point - result);
            }
        }
    }
}