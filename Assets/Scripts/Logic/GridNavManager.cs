using System.Collections.Generic;
using UnityEngine;

public struct GridNavAgentParam
{
    public float mass;
    public float radius;
    public float maxSpeed;
    public float maxAcc;
    public float maxTurnAngle;
}

public enum GridNavAgentState { None, Requesting, WaitForPath, Moving }

public class GridNavAgent
{
    public int id;
    public GridNavAgentParam param;
    public Vector3 pos;
    public Vector3 frontDir;
    public int unitSize;
    public float minExteriorRadius;
    public float maxInteriorRadius;
    public GridNavAgentState state;
    public int squareIndex;
    public Vector3 goalPos;
    public float goalRadius;
    public int goalSquareIndex;
    public List<int> path;
    public float speed;
    public Vector3 velocity;
    public Vector3 prefVelocity;
    public Vector3 newVelocity;
    public IGridNavQueryFilter filter;
    public IGridNavQueryFilter pathFilter;
    public int tempNum;
}

public class GridNavManager
{
    private static float maxAvoideeCosine = Mathf.Cos(120.0f * Mathf.Deg2Rad);
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
        int unitSize = (int)(param.radius * 2 / navMesh.SquareSize - 0.001f) + 1;
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
            minExteriorRadius = 1.41421356237f * unitSize * 0.5f * navMesh.SquareSize,
            maxInteriorRadius = unitSize * 0.5f * navMesh.SquareSize,
            state = GridNavAgentState.None,
            squareIndex = 0,
            goalPos = Vector3.zero,
            goalRadius = 0.0f,
            goalSquareIndex = 0,
            speed = 0.0f,
            velocity = Vector3.zero, //可能有y轴的方向
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
                    if (squareAgent != agent && squareAgent.speed == 0.0f)
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
            if (agent.state != GridNavAgentState.Moving)
            {
                agent.speed = Mathf.Max(0.0f, agent.speed - agent.param.maxAcc * deltaTime);
                continue;
            }
            Debug.Assert(agent.path.Count > 0);
            var pathSquareIndex = agent.path[agent.path.Count - 1];
            var goalPos = pathSquareIndex == agent.goalSquareIndex ? agent.goalPos : navMesh.GetSquarePos(pathSquareIndex);
            if (GridNavMath.SqrDistance2D(agent.pos, goalPos) <= agent.goalRadius * agent.goalRadius)
            {
                agent.speed = Mathf.Max(0.0f, agent.speed - agent.param.maxAcc * deltaTime);
                agent.state = GridNavAgentState.None;
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
                StartMoving(agent, agent.goalPos, agent.goalRadius);
                continue;
            }
            if (!navQuery.Raycast(agent.pathFilter, agent.squareIndex, agent.path[0], out var path, out var totalCost))
            {
                var constraint = new GridNavQueryConstraintCircleStrict(agent.path[0], agent.squareIndex, 16.0f * navMesh.SquareSize);
                if (!navQuery.FindPath(agent.pathFilter, agent.squareIndex, constraint, out path) || path[path.Count - 1] != agent.path[0])
                {
                    StartMoving(agent, agent.goalPos, agent.goalRadius);
                    continue;
                }
            }
            var nextSquareIndex = path.Count > 1 ? path[1] : path[0];
            var nextPos = nextSquareIndex == agent.goalSquareIndex ? agent.goalPos : navMesh.GetSquarePos(nextSquareIndex);
            var disiredDir = GridNavMath.Normalized2D(nextPos - agent.pos);
            agent.prefVelocity = disiredDir * agent.param.maxSpeed;
            //disiredDir = GetObstacleAvoidanceDir(agent, disiredDir);
            //todo 判断剩余距离还有转向速度
            //agent.frontDir = GridNavMath.Rotate2D(agent.frontDir, disiredDir, agent.param.maxTurnAngle * deltaTime);
            //agent.speed = Mathf.Min(agent.param.maxSpeed, agent.speed + agent.param.maxAcc * deltaTime);
        }
        //更新坐标
        foreach (var a in agents)
        {
            var agent = a.Value;
            agent.newVelocity = agent.prefVelocity;
            agent.velocity = agent.newVelocity;
            agent.pos += agent.velocity * deltaTime;
            var newSquareIndex = navMesh.GetSquareIndex(agent.pos);
            if (newSquareIndex != agent.squareIndex)
            {
                RemoveSquareAgent(agent.squareIndex, agent);
                AddSquareAgent(newSquareIndex, agent);
                agent.squareIndex = newSquareIndex;
            }
        }
        //foreach (var a in agents) //移动
        //{
        //    var agent = a.Value;
        //    var oldPos = agent.pos;
        //    if (agent.speed > 0.0f)
        //    {
        //        agent.velocity = agent.speed * agent.frontDir; //todo 暂不考虑y轴
        //        navMesh.ClampInBounds(agent.pos + agent.velocity * deltaTime, out var nextSquareIndex, out var nextPos);
        //        if (!navQuery.Raycast(agent.pathFilter, agent.squareIndex, nextSquareIndex, out var path, out var totalCost))
        //        {
        //            if (path.Count > 0 && path[path.Count - 1] != agent.squareIndex)
        //            {
        //                agent.pos = navMesh.GetSquarePos(path[path.Count - 1]);
        //            }
        //        }
        //        else
        //        {
        //            agent.pos = nextPos;
        //        }
        //    }
        //    HandleObjectCollisions(agent, oldPos);
        //    var newSquareIndex = navMesh.GetSquareIndex(agent.pos);
        //    if (newSquareIndex != agent.squareIndex)
        //    {
        //        RemoveSquareAgent(agent.squareIndex, agent);
        //        AddSquareAgent(newSquareIndex, agent);
        //        agent.squareIndex = newSquareIndex;
        //    }
        //}
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
    }
    public void HandleObjectCollisions(GridNavAgent collider, Vector3 oldPos)
    {
        this.tempNum++;
        float collisionsRadius = collider.speed + collider.maxInteriorRadius * 2.0f;
        navMesh.GetSquareXZ(new Vector3(collider.pos.x - collisionsRadius, 0, collider.pos.z - collisionsRadius), out var sx, out var sz);
        navMesh.GetSquareXZ(new Vector3(collider.pos.x + collisionsRadius, 0, collider.pos.z + collisionsRadius), out var ex, out var ez);
        for (int z = sz; z <= ez; z++)
        {
            for (int x = sx; x <= ex; x++)
            {
                if (!squareAgents.TryGetValue(navMesh.GetSquareIndex(x, z), out var agentList))
                {
                    continue;
                }
                foreach (var collidee in agentList)
                {
                    if (collidee.tempNum == this.tempNum || collidee == collider)
                    {
                        continue;
                    }
                    collidee.tempNum = this.tempNum;
                    var separationRadius = collider.maxInteriorRadius + collidee.maxInteriorRadius;
                    if (GridNavMath.SqrDistance2D(collider.pos, collidee.pos) >= separationRadius * separationRadius)
                    {
                        continue;
                    }
                    Vector3 separationVec = new Vector3(collider.pos.x - collidee.pos.x, 0.0f, collider.pos.z - collidee.pos.z);
                    bool pushCollider = collider.speed > 0.0f;
                    bool pushCollidee = collidee.speed > 0.0f;
                    if (!pushCollider && !pushCollidee)
                    {
                        //var colRadiusSum = collider.maxInteriorRadius + collidee.maxInteriorRadius;
                        //var sepDistance = separationVec.magnitude + 0.1f;
                        //var penDistance = Mathf.Min(0.0f, sepDistance - colRadiusSum);
                        //var rgt = Vector3.Cross(collider.frontDir, Vector3.up);
                        //var colSlideSign = GridNavMath.Dot2D(collidee.pos, rgt) - GridNavMath.Dot2D(collider.pos, rgt) > 0.0f ? -1.0f : 1.0f;
                        //var strafeScale = Mathf.Min(collider.speed, Mathf.Max(0.0f, -penDistance * 0.5f));
                        //var bounceScale = Mathf.Min(collider.speed, Mathf.Max(0.0f, -penDistance));

                        //var strafeVec = (rgt * colSlideSign) * strafeScale;
                        //var bounceVec = (separationVec / sepDistance) * bounceScale;
                        //var summedVec = strafeVec + bounceVec;

                        //var summedSquareIndex = navMesh.GetSquareIndex(collider.pos + summedVec);
                        //if (!collider.filter.IsBlocked(navMesh, summedSquareIndex)) //todo test move square 
                        //{
                        //    collider.pos += summedVec;
                        //}
                        //else
                        //{
                        //    collider.pos = oldPos + summedVec * 0.25f * (GridNavMath.Dot2D(collider.frontDir, separationVec) < 0.25f ? 1.0f : 0.0f);
                        //}
                    }
                    else
                    {
                        //float colliderRelRadius = colliderParams.y / (colliderParams.y + collideeParams.y);
                        //float collideeRelRadius = collideeParams.y / (colliderParams.y + collideeParams.y);
                        float collisionRadiusSum = collider.maxInteriorRadius + collidee.maxInteriorRadius;

                        float sepDistance = separationVec.magnitude;
                        float penDistance = Mathf.Max(collisionRadiusSum - sepDistance, 0.001f);
                        float sepResponse = penDistance * 0.5f;

                        Vector3 sepDirection = separationVec / sepDistance;
                        Vector3 colResponseVec = new Vector3(sepDirection.x, 0, sepDirection.z) * sepResponse;

                        float m1 = collider.param.mass;
                        float m2 = collidee.param.mass;
                        float v1 = collider.speed;
                        float v2 = collidee.speed;
                        float c1 = 1.0f + (1.0f - Mathf.Abs(GridNavMath.Dot2D(collider.frontDir, -sepDirection))) * 5.0f;
                        float c2 = 1.0f + (1.0f - Mathf.Abs(GridNavMath.Dot2D(collidee.frontDir, sepDirection))) * 5.0f;
                        float s1 = m1 * v1 * c1;
                        float s2 = m2 * v2 * c2;
                        float r1 = s1 / (s1 + s2 + 1.0f);
                        float r2 = s2 / (s1 + s2 + 1.0f);

                        float colliderMassScale = Mathf.Clamp(1.0f - r1, 0.01f, 0.99f);
                        float collideeMassScale = Mathf.Clamp(1.0f - r2, 0.01f, 0.99f);
                        var colliderRightDir = Vector3.Cross(collider.frontDir, Vector3.up);
                        var collideeRightDir = Vector3.Cross(collidee.frontDir, Vector3.up);
                        float colliderSlideSign = GridNavMath.Dot2D(separationVec, colliderRightDir) > 0.0f ? 1.0f : -1.0f;
                        float collideeSlideSign = GridNavMath.Dot2D(-separationVec, collideeRightDir) > 0.0f ? 1.0f : -1.0f;

                        Vector3 colliderPushVec = colResponseVec * colliderMassScale;
                        Vector3 collideePushVec = -colResponseVec * collideeMassScale;
                        Vector3 colliderSlideVec = colliderRightDir * colliderSlideSign * ((penDistance)) * r2;
                        Vector3 collideeSlideVec = collideeRightDir * collideeSlideSign * ((penDistance)) * r1;
                        Vector3 colliderMoveVec = colliderPushVec + colliderSlideVec;
                        Vector3 collideeMoveVec = collideePushVec + collideeSlideVec;

                        if (pushCollider)
                        {
                            var tIndex = navMesh.GetSquareIndex(collider.pos + colliderMoveVec);
                            if (!collider.filter.IsBlocked(navMesh, tIndex)) //todo test move square 
                            {
                                collider.pos += colliderMoveVec;
                            }
                        }
                        if (pushCollidee)
                        {
                            var tIndex = navMesh.GetSquareIndex(collidee.pos + collideeMoveVec);
                            if (!collidee.filter.IsBlocked(navMesh, tIndex)) //todo test move square 
                            {
                                collidee.pos += collideeMoveVec;
                            }
                        }
                    }
                }
            }
        }
    }
    public Vector3 GetObstacleAvoidanceDir(GridNavAgent avoider, Vector3 desiredDir)
    {
        if (GridNavMath.Dot2D(avoider.frontDir, desiredDir) < 0.0f) //当前方向与期望方向相反
        {
            return desiredDir;
        }
        this.tempNum++;
        float avoidanceRadius = avoider.minExteriorRadius + avoider.param.maxSpeed * 2.0f;
        Vector3 avoidanceVec = Vector3.zero;
        navMesh.GetSquareXZ(new Vector3(avoider.pos.x - avoidanceRadius, 0, avoider.pos.z - avoidanceRadius), out var sx, out var sz);
        navMesh.GetSquareXZ(new Vector3(avoider.pos.x + avoidanceRadius, 0, avoider.pos.z + avoidanceRadius), out var ex, out var ez);
        for (int z = sz; z <= ez; z++)
        {
            for (int x = sx; x <= ex; x++)
            {
                if (!squareAgents.TryGetValue(navMesh.GetSquareIndex(x, z), out var agentList))
                {
                    continue;
                }
                foreach (var avoidee in agentList)
                {
                    if (avoidee.tempNum == this.tempNum || avoidee == avoider)
                    {
                        continue;
                    }
                    avoidee.tempNum = this.tempNum;
                    if (avoidee.speed <= 0.0f) //寻路的时候，已排除未移动的物体
                    {
                        continue;
                    }
                    Vector3 avoideeVector = (avoider.pos + avoider.velocity) - (avoidee.pos + avoidee.velocity);
                    float avoideeDist = avoideeVector.magnitude + 0.01f;
                    float avoidanceRadiusSum = avoider.minExteriorRadius + avoidee.minExteriorRadius;
                    if (avoideeDist >= avoider.param.maxSpeed + avoidanceRadiusSum) //筛选距离
                    {
                        continue;
                    }
                    if (GridNavMath.Dot2D(avoider.frontDir, -(avoideeVector / avoideeDist)) < maxAvoideeCosine)//忽略与碰撞体偏离中心度数过大的对象
                    {
                        continue;
                    }
                    if (avoideeDist * avoideeDist >= (avoider.pos - avoider.goalPos).sqrMagnitude) //如果avoider离目标点距离小于碰撞避免距离
                    {
                        continue;
                    }
                    Vector3 avoiderRightDir = Vector3.Cross(avoider.frontDir, Vector3.up);
                    Vector3 avoideeRightDir = Vector3.Cross(avoidee.frontDir, Vector3.up);
                    float avoiderTurnSign = Vector3.Dot(avoidee.pos, avoiderRightDir) - Vector3.Dot(avoider.pos, avoiderRightDir) > 0.0f ? -1.0f : 1.0f;
                    float avoideeTurnSign = Vector3.Dot(avoider.pos, avoideeRightDir) - Vector3.Dot(avoidee.pos, avoideeRightDir) > 0.0f ? -1.0f : 1.0f;

                    float avoidanceCosAngle = Mathf.Clamp(Vector3.Dot(avoider.frontDir, avoidee.frontDir), -1.0f, 1.0f);
                    float avoidanceResponse = (1.0f - avoidanceCosAngle) + 0.1f;
                    float avoidanceFallOff = (1.0f - Mathf.Min(1.0f, avoideeDist / (5.0f * avoidanceRadiusSum)));

                    if (avoidanceCosAngle < 0.0f)
                    {
                        avoiderTurnSign = Mathf.Max(avoiderTurnSign, avoideeTurnSign);
                    }
                    float avoidanceMassSum = avoider.param.mass + avoidee.param.mass;
                    float avoideeMassScale = avoidee.param.mass / avoidanceMassSum;
                    avoidanceVec += (avoiderRightDir * 1.0f * avoiderTurnSign * avoidanceResponse * avoidanceFallOff * avoideeMassScale);
                }
            }
        }
        Vector3 avoidanceDir = Vector3.Lerp(desiredDir, avoidanceVec, 0.5f).normalized;
        avoidanceDir = Vector3.Lerp(avoidanceDir, desiredDir, 0.7f).normalized;
        return avoidanceDir;
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
}