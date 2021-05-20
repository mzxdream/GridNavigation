using System.Collections.Generic;
using UnityEngine;

namespace GridNav
{
    public struct NavRVOLine
    {
        public Vector3 point;
        public Vector3 direction;
    }

    public class NavRVOObstacle
    {
        public Vector3 point;
        public Vector3 direction;
        public bool isConvex;
        public NavRVOObstacle prev;
        public NavRVOObstacle next;
    }

    public static class NavRVO
    {
        private static void ComputeNewVelocity(NavAgent agent, List<NavRVOObstacle> obstacles, List<NavAgent> neighbors, float deltaTime)
        {
            var orcaLines = new List<NavRVOLine>();

            float timeHorizonObst = 1.0f; // TODO
            float invTimeHorizonObst = 1.0f / timeHorizonObst;

            float radius = agent.maxInteriorRadius;
            var position = agent.pos;
            var velocity = agent.velocity;
            /* Create obstacle ORCA lines. */
            for (int i = 0; i < obstacles.Count; ++i)
            {
                var obstacle1 = obstacles[i];
                var obstacle2 = obstacle1.next;

                Vector3 relativePosition1 = obstacle1.point - position;
                Vector3 relativePosition2 = obstacle2.point - position;

                /*
                 * Check if velocity obstacle of obstacle is already taken care
                 * of by previously constructed obstacle ORCA lines.
                */
                bool alreadyCovered = false;

                for (int j = 0; j < orcaLines.Count; ++j)
                {
                    if (NavMathUtils.Det2D(invTimeHorizonObst * relativePosition1 - orcaLines[j].point, orcaLines[j].direction) - invTimeHorizonObst * radius >= -NavMathUtils.EPSILON
                         && NavMathUtils.Det2D(invTimeHorizonObst * relativePosition2 - orcaLines[j].point, orcaLines[j].direction) - invTimeHorizonObst * radius >= -NavMathUtils.EPSILON)
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
                float distSq1 = NavMathUtils.SqrMagnitude2D(relativePosition1);
                float distSq2 = NavMathUtils.SqrMagnitude2D(relativePosition2);

                float radiusSq = radius * radius;

                Vector3 obstacleVector = obstacle2.point - obstacle1.point;
                float s = NavMathUtils.Dot2D(-relativePosition1, obstacleVector) / NavMathUtils.SqrMagnitude2D(obstacleVector);
                float distSqLine = NavMathUtils.SqrMagnitude2D(-relativePosition1 - s * obstacleVector);

                NavRVOLine line;

                if (s < 0.0f && distSq1 <= radiusSq)
                {
                    /* Collision with left vertex. Ignore if non-convex. */
                    if (obstacle1.isConvex)
                    {
                        line.point = Vector3.zero;
                        line.direction = NavMathUtils.Normalized2D(new Vector3(-relativePosition1.z, 0, relativePosition1.x));
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
                    if (obstacle2.isConvex && NavMathUtils.Det2D(relativePosition2, obstacle2.direction) >= 0.0f)
                    {
                        line.point = Vector3.zero;
                        line.direction = NavMathUtils.Normalized2D(new Vector3(-relativePosition2.z, 0, relativePosition2.x));
                        orcaLines.Add(line);
                    }

                    continue;
                }
                else if (s >= 0.0f && s <= 1.0f && distSqLine <= radiusSq)
                {
                    /* Collision with obstacle segment. */
                    line.point = Vector3.zero;
                    line.direction = -obstacle1.direction;
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
                    if (!obstacle1.isConvex)
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
                    if (!obstacle2.isConvex)
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
                    if (obstacle1.isConvex)
                    {
                        float leg1 = Mathf.Sqrt(distSq1 - radiusSq);
                        leftLegDirection = new Vector3(relativePosition1.x * leg1 - relativePosition1.z * radius, 0, relativePosition1.x * radius + relativePosition1.z * leg1) / distSq1;
                    }
                    else
                    {
                        /* Left vertex non-convex; left leg extends cut-off line. */
                        leftLegDirection = -obstacle1.direction;
                    }

                    if (obstacle2.isConvex)
                    {
                        float leg2 = Mathf.Sqrt(distSq2 - radiusSq);
                        rightLegDirection = new Vector3(relativePosition2.x * leg2 + relativePosition2.z * radius, 0, -relativePosition2.x * radius + relativePosition2.z * leg2) / distSq2;
                    }
                    else
                    {
                        /* Right vertex non-convex; right leg extends cut-off line. */
                        rightLegDirection = obstacle1.direction;
                    }
                }

                /*
                 * Legs can never point into neighboring edge when convex
                 * vertex, take cutoff-line of neighboring edge instead. If
                 * velocity projected on "foreign" leg, no constraint is added.
                 */

                var leftNeighbor = obstacle1.prev;

                bool isLeftLegForeign = false;
                bool isRightLegForeign = false;

                if (obstacle1.isConvex && NavMathUtils.Det2D(leftLegDirection, -leftNeighbor.direction) >= 0.0f)
                {
                    /* Left leg points into obstacle. */
                    leftLegDirection = -leftNeighbor.direction;
                    isLeftLegForeign = true;
                }

                if (obstacle2.isConvex && NavMathUtils.Det2D(rightLegDirection, obstacle2.direction) <= 0.0f)
                {
                    /* Right leg points into obstacle. */
                    rightLegDirection = obstacle2.direction;
                    isRightLegForeign = true;
                }

                /* Compute cut-off centers. */
                Vector3 leftCutOff = invTimeHorizonObst * (obstacle1.point - position);
                Vector3 rightCutOff = invTimeHorizonObst * (obstacle2.point - position);
                Vector3 cutOffVector = rightCutOff - leftCutOff;

                /* Project current velocity on velocity obstacle. */

                /* Check if current velocity is projected on cutoff circles. */
                float t = obstacle1 == obstacle2 ? 0.5f : NavMathUtils.Dot2D(velocity - leftCutOff, cutOffVector) / NavMathUtils.SqrMagnitude2D(cutOffVector);
                float tLeft = NavMathUtils.Dot2D(velocity - leftCutOff, leftLegDirection);
                float tRight = NavMathUtils.Dot2D(velocity - rightCutOff, rightLegDirection);

                if ((t < 0.0f && tLeft < 0.0f) || (obstacle1 == obstacle2 && tLeft < 0.0f && tRight < 0.0f))
                {
                    /* Project on left cut-off circle. */
                    Vector3 unitW = NavMathUtils.Normalized2D(velocity - leftCutOff);

                    line.direction = new Vector3(unitW.z, 0, -unitW.x);
                    line.point = leftCutOff + radius * invTimeHorizonObst * unitW;
                    orcaLines.Add(line);

                    continue;
                }
                else if (t > 1.0f && tRight < 0.0f)
                {
                    /* Project on right cut-off circle. */
                    Vector3 unitW = NavMathUtils.Normalized2D(velocity - rightCutOff);

                    line.direction = new Vector3(unitW.z, 0, -unitW.x);
                    line.point = rightCutOff + radius * invTimeHorizonObst * unitW;
                    orcaLines.Add(line);

                    continue;
                }

                /*
                 * Project on left leg, right leg, or cut-off line, whichever is
                 * closest to velocity.
                 */
                float distSqCutoff = (t < 0.0f || t > 1.0f || obstacle1 == obstacle2) ? float.PositiveInfinity : NavMathUtils.SqrMagnitude2D(velocity - (leftCutOff + t * cutOffVector));
                float distSqLeft = tLeft < 0.0f ? float.PositiveInfinity : NavMathUtils.SqrMagnitude2D(velocity - (leftCutOff + tLeft * leftLegDirection));
                float distSqRight = tRight < 0.0f ? float.PositiveInfinity : NavMathUtils.SqrMagnitude2D(velocity - (rightCutOff + tRight * rightLegDirection));

                if (distSqCutoff <= distSqLeft && distSqCutoff <= distSqRight)
                {
                    /* Project on cut-off line. */
                    line.direction = -obstacle1.direction;
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

            int numObstLines = orcaLines.Count;

            var timeHorizon = 1.0f; // TODO
            float invTimeHorizon = 1.0f / timeHorizon;

            /* Create agent ORCA lines. */
            for (int i = 0; i < neighbors.Count; ++i)
            {
                var other = neighbors[i];
                // mass
                float massRatio = other.param.mass / (agent.param.mass + other.param.mass);
                float neighborMassRatio = agent.param.mass / (agent.param.mass + other.param.mass);
                Vector3 velocityOpt = (massRatio >= 0.5f ? (agent.velocity - massRatio * agent.velocity) * 2 : agent.prefVelocity + (agent.velocity - agent.prefVelocity) * massRatio * 2);
                Vector3 neighborVelocityOpt = (neighborMassRatio >= 0.5f ? 2 * other.velocity * (1 - neighborMassRatio) : other.prefVelocity + (other.velocity - other.prefVelocity) * neighborMassRatio * 2);

                Vector3 relativePosition = other.pos - position;
                Vector3 relativeVelocity = velocityOpt - neighborVelocityOpt; // origin is  Vector3 relativeVelocity = velocity - other.velocity;
                float distSq = NavMathUtils.SqrMagnitude2D(relativePosition);
                float combinedRadius = radius + other.maxInteriorRadius;
                float combinedRadiusSq = combinedRadius * combinedRadius;

                NavRVOLine line;
                Vector3 u;

                if (distSq > combinedRadiusSq)
                {
                    /* No collision. */
                    Vector3 w = relativeVelocity - invTimeHorizon * relativePosition;

                    /* Vector from cutoff center to relative velocity. */
                    float wLengthSq = NavMathUtils.SqrMagnitude2D(w);
                    float dotProduct1 = NavMathUtils.Dot2D(w, relativePosition);

                    if (dotProduct1 < 0.0f && dotProduct1 * dotProduct1 > combinedRadiusSq * wLengthSq)
                    {
                        /* Project on cut-off circle. */
                        float wLength = Mathf.Sqrt(wLengthSq);
                        Vector3 unitW = w / wLength;

                        line.direction = new Vector3(unitW.z, 0, -unitW.x);
                        u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                    }
                    else
                    {
                        /* Project on legs. */
                        float leg = Mathf.Sqrt(distSq - combinedRadiusSq);

                        if (NavMathUtils.Det2D(relativePosition, w) > 0.0f)
                        {
                            /* Project on left leg. */
                            line.direction = new Vector3(relativePosition.x * leg - relativePosition.z * combinedRadius, 0.0f, relativePosition.x * combinedRadius + relativePosition.z * leg) / distSq;
                        }
                        else
                        {
                            /* Project on right leg. */
                            line.direction = -new Vector3(relativePosition.x * leg + relativePosition.z * combinedRadius, 0.0f, -relativePosition.x * combinedRadius + relativePosition.z * leg) / distSq;
                        }

                        float dotProduct2 = NavMathUtils.Dot2D(relativeVelocity, line.direction);
                        u = dotProduct2 * line.direction - relativeVelocity;
                    }
                }
                else
                {
                    /* Collision. Project on cut-off circle of time timeStep. */
                    float invTimeStep = 1.0f / deltaTime;

                    /* Vector from cutoff center to relative velocity. */
                    Vector3 w = relativeVelocity - invTimeStep * relativePosition;

                    float wLength = NavMathUtils.Magnitude2D(w);
                    Vector3 unitW = w / wLength;

                    line.direction = new Vector3(unitW.z, 0, -unitW.x);
                    u = (combinedRadius * invTimeStep - wLength) * unitW;
                }

                line.point = velocityOpt + massRatio * u; // origin is line.point = velocity + 0.5f * u;
                orcaLines.Add(line);
            }

            int lineFail = linearProgram2(orcaLines, agent.param.maxSpeed, agent.prefVelocity, false, ref agent.newVelocity);

            if (lineFail < orcaLines.Count)
            {
                linearProgram3(orcaLines, numObstLines, lineFail, agent.param.maxSpeed, ref agent.newVelocity);
            }
        }
        private static bool linearProgram1(IList<Line> lines, int lineNo, float radius, Vector3 optVelocity, bool directionOpt, ref Vector3 result)
        {
            float dotProduct = NavMathUtils.Dot2D(lines[lineNo].point, lines[lineNo].direction);
            float discriminant = dotProduct * dotProduct + radius * radius - NavMathUtils.SqrMagnitude2D(lines[lineNo].point);

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
                float denominator = NavMathUtils.Det2D(lines[lineNo].direction, lines[i].direction);
                float numerator = NavMathUtils.Det2D(lines[i].direction, lines[lineNo].point - lines[i].point);

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
                if (NavMathUtils.Dot2D(optVelocity, lines[lineNo].direction) > 0.0f)
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
                float t = NavMathUtils.Dot2D(lines[lineNo].direction, (optVelocity - lines[lineNo].point));

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
            else if (NavMathUtils.SqrMagnitude2D(optVelocity) > radius * radius)
            {
                /* Optimize closest point and outside circle. */
                // 2.当 optVelocity 太大时，先归一化optVelocity，再乘 radius
                result = NavMathUtils.Normalized2D(optVelocity) * radius;
            }
            else
            {
                /* Optimize closest point and inside circle. */
                // 3.当 optVelocity 小于maxSpeed时
                result = optVelocity;
            }

            for (int i = 0; i < lines.Count; ++i)
            {
                if (NavMathUtils.Det2D(lines[i].direction, lines[i].point - result) > 0.0f)
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
                if (NavMathUtils.Det2D(lines[i].direction, lines[i].point - result) > distance)
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

                        float determinant = NavMathUtils.Det2D(lines[i].direction, lines[j].direction);

                        if (Mathf.Abs(determinant) <= 0.00001f)
                        {
                            /* Line i and line j are parallel. */
                            if (NavMathUtils.Dot2D(lines[i].direction, lines[j].direction) > 0.0f)
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
                            line.point = lines[i].point + (NavMathUtils.Det2D(lines[j].direction, lines[i].point - lines[j].point) / determinant) * lines[i].direction;
                        }
                        // 计算ORCA线的方向
                        line.direction = NavMathUtils.Normalized2D(lines[j].direction - lines[i].direction);
                        projLines.Add(line);
                    }
                    // 3.再次计算最优速度
                    Vector3 tempResult = result;
                    // 注意这里的 new Vector3(-lines[i].direction.y(), lines[i].direction.x()) 是方向向量
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

                    distance = NavMathUtils.Det2D(lines[i].direction, lines[i].point - result);
                }
            }
        }

    }
}