using UnityEngine;

public class GridMoveAgentParam
{
    public int teamID; //enemy can't push
    public int unitSize;
    public float mass; //calc push distance
    public float maxSpeed;
    public float maxAcc;
    public float maxDec;
    public bool isPushResistant;
}

public class GridMoveAgent
{
    private enum ProgressState { Done = 0, Active = 1, Failed = 2 };
    GridMoveManager manager;
    private int id;
    private GridMoveAgentParam param;
    private Vector3 pos;
    private Vector3 forward;
    private int gridIndex;

    private float maxSpeed;
    private float accRate;
    private float decRate;
    private Vector3 currentVelocity;
    private float currentSpeed;
    private float deltaSpeed;

    private float turnRate;
    private float turnAcc;

    private Vector3 goalPos;
    private float goalRadius;
    private bool atGoal;
    private bool atEndOfPath;

    private ProgressState progressState;
    private bool isWantRepath;
    private GridPath path;
    private Vector3 currWayPoint;
    private Vector3 nextWayPoint;
    private bool idling;
    private int numIdlingUpdates;
    private int numIdlingSlowUpdates;
    private Vector3 waypointDir;
    private Vector3 oldPos;

    public Vector3 Pos { get => pos; }
    public Vector3 Forward { get => forward; }
    public int UnitSize { get => param.unitSize; }
    public bool WantToStop { get => path == null && atEndOfPath; }

    public GridMoveAgent(GridMoveManager manager)
    {
        this.manager = manager;
    }
    public bool Init(int id, Vector3 pos, Vector3 forward, GridMoveAgentParam param)
    {
        this.id = id;
        this.param = param;
        this.pos = manager.ClampInBounds(pos);
        this.forward = forward;
        this.gridIndex = manager.GetGridIndex(this.pos);

        this.maxSpeed = param.maxSpeed / manager.GameSpeed;
        this.accRate = Mathf.Max(0.01f, param.maxAcc);
        this.decRate = Mathf.Max(0.01f, param.maxDec);
        this.currentVelocity = Vector3.zero;
        this.currentSpeed = 0f;
        this.deltaSpeed = 0f;

        this.turnRate = 0f;
        this.turnAcc = 0f;

        this.goalPos = this.pos;
        this.goalRadius = 0.01f;
        this.atGoal = true;
        this.atEndOfPath = true;

        this.progressState = ProgressState.Done;
        this.isWantRepath = false;
        this.currWayPoint = new Vector3();
        this.nextWayPoint = new Vector3();

        return true;
    }
    public void Clear()
    {
    }
    private void ChangeHeading(Vector3 newWantedForward)
    {
        //todo turn rate
        forward = newWantedForward;
    }
    private static float BrakingDistance(float speed, float rate)
    {
        float t = speed / Mathf.Max(rate, 0.001f);
        float d = 0.5f * rate * t * t;
        return d;
    }
    private void ChangeSpeed(float newWantedSpeed)
    {
        if (newWantedSpeed <= 0.0f && currentSpeed < 0.01f)
        {
            currentSpeed = 0.0f;
            deltaSpeed = 0.0f;
            return;
        }
        float targetSpeed = maxSpeed;
        if (currWayPoint.y == -1.0f && nextWayPoint.y == -1.0f)
        {
            targetSpeed = 0.0f;
        }
        else
        {
            if (newWantedSpeed > 0.0f)
            {
                if (!WantToStop)
                {
                    float curGoalDistSq = (pos - goalPos).sqrMagnitude;
                    float minGoalDist = BrakingDistance(currentSpeed, decRate);
                    if (curGoalDistSq > minGoalDist * minGoalDist)
                    {
                        //TODO check turn speed
                    }
                    else
                    {
                        targetSpeed = 0.0f;

                    }
                }
                else
                {
                    targetSpeed = 0.0f;
                }
            }
            else
            {
                targetSpeed = 0.0f;
            }
        }
        targetSpeed = Mathf.Min(targetSpeed, newWantedSpeed);
        float speedDiff = targetSpeed - currentSpeed;
        if (speedDiff > 0.0f)
        {
            deltaSpeed = Mathf.Min(speedDiff, accRate);
        }
        else
        {
            deltaSpeed = Mathf.Max(speedDiff, -decRate);
        }
    }
    private bool CanSetNextWayPoint()
    {
        if (path == null)
        {
            return false;
        }
        float currWayPointDist = GridMathUtils.Distance2D(pos, currWayPoint);
        //TODO check turn rate
        if (currWayPointDist > Mathf.Max(currentSpeed * 1.05f, manager.GridSize) && Vector3.Dot(waypointDir, forward) >= 0.995f)
        {
            return false;
        }
        ;
        if (currWayPointDist > manager.GridSize
            && !manager.TestMoveRange(this, Vector3.Min(pos, currWayPoint), Vector3.Max(pos, currWayPoint), true))
        {
            return false;
        }
        atEndOfPath = GridMathUtils.SqrDistance2D(currWayPoint, goalPos) <= goalRadius * goalRadius;
        if (atEndOfPath)
        {
            currWayPoint = goalPos;
            nextWayPoint = goalPos;
        }
        return true;
    }
    private void SetNextWayPoint()
    {
        if (CanSetNextWayPoint())
        {
            currWayPoint = nextWayPoint;
            nextWayPoint = manager.NextWayPoint(this, this.path, currWayPoint, Mathf.Max(currentSpeed * 1.05f, 1.25f * manager.GridSize));
            //check nextwaypoint is success
            //if ()
            //{
            //    Fail(false);
            //}
        }
        if (manager.IsGridBlocked(this, currWayPoint, true) || manager.IsGridBlocked(this, nextWayPoint, true))
        {
            ReRequestPath(false);
        }
    }
    private void Arrived(bool call)
    {
        if (progressState == ProgressState.Active)
        {
            StopEngine(call, false);
            progressState = ProgressState.Done;
        }
    }
    private void Fail(bool call)
    {
        StopEngine(call, false);
        progressState = ProgressState.Failed;
    }
    private void StartEngine(bool call)
    {
        if (path == null)
        {
            path = manager.FindPath(this, this.goalPos, this.goalRadius);
            if (path != null)
            {
                atGoal = false;
                atEndOfPath = false;
                currWayPoint = manager.NextWayPoint(this, this.path, pos, Mathf.Max(currentSpeed * 1.05f, 1.25f * manager.GridSize));
                nextWayPoint = manager.NextWayPoint(this, this.path, currWayPoint, Mathf.Max(currentSpeed * 1.05f, 1.25f * manager.GridSize));
            }
            else
            {
                Fail(false);
            }
        }
    }
    private void StopEngine(bool call, bool hardStop)
    {
        if (path != null)
        {
            path = null;
        }
        atEndOfPath = true;
        if (hardStop)
        {
            currentVelocity = Vector3.zero;
            currentSpeed = 0f;
        }
    }
    private void ReRequestPath(bool forceRequest)
    {
        if (forceRequest)
        {
            isWantRepath = false;
            StopEngine(false, false);
            StartEngine(false);
            return;
        }
        isWantRepath = true;
    }
    private Vector3 GetObstacleAvoidanceDir(Vector3 desiredDir)
    {
        return desiredDir;
    }
    private void UpdateOwnerAccelAndHeading()
    {
        if (WantToStop)
        {
            currWayPoint.y = -1.0f;
            nextWayPoint.y = -1.0f;
            ChangeHeading(forward);
            ChangeSpeed(0.0f);
        }
        else
        {
            float curGoalDistSq = (pos - goalPos).sqrMagnitude;
            atGoal |= curGoalDistSq <= goalRadius * goalRadius;
            if (curGoalDistSq <= currentSpeed * 1.05f * currentSpeed * 1.05f)
            {
                atGoal |= Vector3.Dot(forward, goalPos - pos) > 0.0f && Vector3.Dot(forward, goalPos - (pos + currentVelocity)) <= 0.0f;
            }
            if (!atGoal)
            {
                if (idling)
                {
                    numIdlingUpdates = Mathf.Min(numIdlingUpdates + 1, 32768);
                }
                else
                {
                    numIdlingUpdates = Mathf.Max(numIdlingUpdates - 1, 0);
                }
            }
            if (!atEndOfPath)
            {
                SetNextWayPoint();
            }
            else
            {
                if (atGoal)
                {
                    Arrived(false);
                }
                else
                {
                    ReRequestPath(false);
                }
            }
            if (Mathf.Abs(currWayPoint.x - pos.x) > 0.0001f || Mathf.Abs(currWayPoint.z - pos.z) > 0.0001f)
            {
                waypointDir = new Vector3(currWayPoint.x - pos.x, 0.0f, currWayPoint.z - pos.z).normalized;
            }
            Vector3 wantedForward = !atGoal ? waypointDir : forward;
            wantedForward = GetObstacleAvoidanceDir(wantedForward);
            ChangeHeading(wantedForward);
            ChangeSpeed(maxSpeed);
        }
    }
    private void UpdateOwnerPos(Vector3 oldVelocity, Vector3 newVelocity)
    {
        if (newVelocity != Vector3.zero)
        {
            Vector3 newPos = pos + newVelocity;
            if (!manager.TestMoveRange(this, newPos, newPos, false))
            {
                Vector3 rightDir = Vector3.Cross(forward, Vector3.up);
                for (int n = 8; n > 0; n--)
                {
                    Vector3 testPos = newPos + rightDir * (manager.GridSize / n);
                    if (manager.TestMoveRange(this, testPos, testPos, false))
                    {
                        pos = testPos;
                        break;
                    }
                    testPos = newPos - rightDir * (manager.GridSize / n);
                    if (manager.TestMoveRange(this, testPos, testPos, false))
                    {
                        pos = testPos;
                        break;
                    }
                }
            }
            else
            {
                pos = newPos;
            }
        }
        currentVelocity = newVelocity;
        currentSpeed = newVelocity.magnitude;
        deltaSpeed = 0.0f;
    }
    private bool IsMoving()
    {
        return currentSpeed < 0.001f;
    }
    private static bool HandleStaticObjectCollision(GridMoveAgent collider, GridMoveAgent collidee, float colliderRadius, float collideeRadius, Vector3 separationVec, bool canRequestPath, bool checkTerrain)
    {
        var manager = collider.manager;
        if (checkTerrain && !collider.IsMoving())
        {
            return false;
        }
        Vector3 pos = collider.pos;
        Vector3 vel = collider.currentVelocity;
        Vector3 rgt = collider.GetRightDir();
        if (checkTerrain)
        {
            var rightDir2D = new Vector3(rgt.x, 0, rgt.z).normalized;
            var speedDir2D = new Vector3(vel.x, 0, vel.z).normalized;

            manager.GetGirdXZ(pos + vel, out int xmid, out int zmid);
            int xsh = collider.UnitSize / 2;
            int zsh = collider.UnitSize / 2;

            int xmin = Mathf.Min(-1, -xsh);
            int xmax = Mathf.Max(1, xsh);
            int zmin = Mathf.Min(-1, -zsh);
            int zmax = Mathf.Max(1, zsh);

            Vector3 strafeVec = new Vector3();
            Vector3 bounceVec = new Vector3();
            Vector3 summedVec = new Vector3();
            Vector3 sqrSumPosition = new Vector3();
            float sqrPenDistanceSum = 0;
            float sqrPenDistanceCount = 0;
            for (int z = zmin; z <= zmax; z++)
            {
                for (int x = xmin; x <= xmax; x++)
                {
                    int xabs = xmid + x;
                    int zabs = zmid + z;
                    if (checkTerrain && !manager.IsGridBlocked(xabs, zabs))
                    {
                        continue;
                    }
                    Vector3 squarePos = manager.GetGridPos(xabs, zabs);
                    Vector3 squareVec = pos - squarePos;
                    if (Vector3.Dot(squareVec, vel) > 0.0f)
                    {
                        continue;
                    }
                    float squareRadius = Mathf.Sqrt(2 * (manager.GridSize / 2) * (manager.GridSize / 2));
                    float squareColRadiusSum = collider.GetRadius() + squareRadius;
                    float squareSepDistance = squareVec.magnitude + 0.1f;
                    float squarePenDistance = Mathf.Min(squareSepDistance - squareColRadiusSum, 0.0f);

                    bounceVec += rightDir2D * Vector3.Dot(rightDir2D, squareVec / squareSepDistance);
                    sqrPenDistanceSum += squarePenDistance;
                    sqrPenDistanceCount += 1.0f;
                    sqrSumPosition += squarePos;
                }
            }
            if (sqrPenDistanceCount > 0.0f)
            {
                sqrSumPosition *= (1.0f / sqrPenDistanceCount);
                sqrPenDistanceSum *= (1.0f / sqrPenDistanceCount);
                sqrPenDistanceCount *= (1.0f / sqrPenDistanceCount);

                float strafeSign = Vector3.Dot(sqrSumPosition, rightDir2D) - Vector3.Dot(pos, rightDir2D) > 0 ? -1 : 1;
                float bounceSign = Vector3.Dot(rightDir2D, bounceVec) > 0 ? 1 : -1;
                float strafeScale = Mathf.Min(collider.param.maxSpeed, Mathf.Max(0.1f, -sqrPenDistanceSum * 0.5f));
                float bounceScale = Mathf.Min(collider.param.maxSpeed, Mathf.Max(0.1f, -sqrPenDistanceSum * 0.5f));

                float fpsStrafeScale = (strafeScale / (strafeScale + bounceScale)) * collider.param.maxSpeed;
                float fpsBounceScale = (bounceScale / (strafeScale + bounceScale)) * collider.param.maxSpeed;

                // bounceVec always points along rightDir by construction
                strafeVec = (rightDir2D * strafeSign) * strafeScale;
                bounceVec = (rightDir2D * bounceSign) * bounceScale;
                summedVec = strafeVec + bounceVec;

                // if checkTerrain is true, test only the center square
                if (manager.TestMoveRange(collider, pos + summedVec, pos + summedVec, false))
                {
                    collider.pos = collider.pos + summedVec;
                    collider.currWayPoint += summedVec;
                    collider.nextWayPoint += summedVec;
                }
                else
                {
                    collider.pos = collider.oldPos;
                }
            }
            return canRequestPath && summedVec != Vector3.zero;
        }
        else
        {
            float colRadiusSum = collider.GetRadius() + collidee.GetRadius();
            float sepDistance = separationVec.magnitude + 0.1f;
            float penDistance = Mathf.Min(sepDistance - colRadiusSum, 0.0f);
            float colSlideSign = Vector3.Dot(collidee.pos, rgt) - Vector3.Dot(pos, rgt) > 0.0f ? -1 : 1;

            float strafeScale = Mathf.Min(collider.currentSpeed, Mathf.Max(0.0f, -penDistance * 0.5f));
            float bounceScale = Mathf.Min(collider.currentSpeed, Mathf.Max(0.0f, -penDistance));

            Vector3 strafeVec = (rgt * colSlideSign) * strafeScale;
            Vector3 bounceVec = (separationVec / sepDistance) * bounceScale;
            Vector3 summedVec = strafeVec + bounceVec;

            if (manager.TestMoveRange(collider, pos + summedVec, pos + summedVec, true))
            {
                collider.pos = collider.pos + summedVec;
                collider.currWayPoint += summedVec;
                collider.nextWayPoint += summedVec;
            }
            else
            {
                collider.pos = collider.oldPos + summedVec * 0.25f * (Vector3.Dot(collider.forward, separationVec) < 0.25f ? 1 : 0);
            }
            // same here
            return (canRequestPath && (penDistance < 0.0f));
        }
    }
    private static void HandleUnitCollisionsAux(GridMoveAgent collider, GridMoveAgent collidee)
    {
        if (!collider.IsMoving() || collider.progressState != ProgressState.Active)
        {
            return;
        }
        if (GridMathUtils.SqrDistance2D(collider.pos, collidee.pos) >= Mathf.PI * Mathf.PI)
        {
            return;
        }
        switch (collidee.progressState)
        {
            case ProgressState.Done:
                {
                    if (collidee.IsMoving())
                    {
                        return;
                    }
                    collider.atGoal = true;
                    collider.atEndOfPath = true;
                }
                break;
            case ProgressState.Active:
                {
                    if (collidee.currWayPoint == collider.nextWayPoint)
                    {
                        collider.currWayPoint.y = -1.0f;
                        return;
                    }
                    if (GridMathUtils.SqrDistance2D(collider.pos, collider.goalPos) >= collider.GetRadius() * collider.GetRadius())
                    {
                        return;
                    }
                    collider.atGoal = true;
                    collider.atEndOfPath = true;
                }
                break;
        }
    }
    private Vector3 GetRightDir()
    {
        return Vector3.Cross(forward, Vector3.up);
    }
    private static void HandleUnitCollisions(GridMoveAgent collider, float colliderSpeed, float colliderRadius)
    {
        var manager = collider.manager;
        foreach (var collidee in manager.GetUnitsExact(collider.pos, colliderSpeed + colliderRadius * 2.0f))
        {
            if (collider == collidee)
            {
                continue;
            }
            //TODO filter
            HandleUnitCollisionsAux(collider, collidee);
            bool pushCollider = !collider.param.isPushResistant;
            bool pushCollidee = !collidee.param.isPushResistant;
            if (collider.param.teamID != collidee.param.teamID)
            {
                pushCollidee = false;
                pushCollider = false;
            }
            Vector3 separationVec = collider.pos - collidee.pos;
            float collideeRadius = collidee.GetRadius();
            if (!pushCollider && !pushCollidee)
            {
                bool allowNewPath = !collider.atEndOfPath && !collider.atGoal;
                if (HandleStaticObjectCollision(collider, collidee, colliderRadius, collideeRadius, separationVec, allowNewPath, false))
                {
                    collider.ReRequestPath(false);
                }
                continue;
            }
            //float colliderRelRadius = colliderRadius / (colliderRadius + collideeRadius);
            //float collideeRelRadius = collideeRadius / (colliderRadius + collideeRadius);
            float collisionRadiusSum = colliderRadius + collideeRadius;
            float sepDistance = separationVec.magnitude + 0.1f;
            float penDistance = Mathf.Max(collisionRadiusSum - sepDistance, 1.0f);
            float sepResponse = Mathf.Min(manager.GridSize * 2.0f, penDistance * 0.5f);

            Vector3 sepDirection = separationVec / sepDistance;
            Vector3 colResponseVec = new Vector3(sepDirection.x, 0, sepDirection.z) * sepResponse;

            float m1 = collider.param.mass;
            float m2 = collidee.param.mass;
            float v1 = Mathf.Max(1.0f, colliderSpeed);
            float v2 = Mathf.Max(1.0f, collidee.currentSpeed);
            float c1 = 1.0f + (1.0f - Mathf.Abs(Vector3.Dot(collider.forward, -sepDirection))) * 5.0f;
            float c2 = 1.0f + (1.0f - Mathf.Abs(Vector3.Dot(collidee.forward, sepDirection))) * 5.0f;
            float s1 = m1 * v1 * c1;
            float s2 = m2 * v2 * c2;
            float r1 = s1 / (s1 + s2 + 1.0f);
            float r2 = s2 / (s1 + s2 + 1.0f);

            float colliderMassScale = Mathf.Clamp(1.0f - r1, 0.01f, 0.99f);
            float collideeMassScale = Mathf.Clamp(1.0f - r2, 0.01f, 0.99f);

            float colliderSlideSign = Vector3.Dot(separationVec, collider.GetRightDir()) > 0 ? 1 : -1;
            float collideeSlideSign = Vector3.Dot(-separationVec, collidee.GetRightDir()) > 0 ? 1 : -1;

            bool ignoreCollidee = collider.param.teamID == collidee.param.teamID && collider.IsMoving() && !collidee.IsMoving();
            Vector3 colliderPushVec = colResponseVec * colliderMassScale * (!ignoreCollidee ? 1 : 0);
            Vector3 collideePushVec = -colResponseVec * collideeMassScale;
            Vector3 colliderSlideVec = collider.GetRightDir() * colliderSlideSign * (1.0f / penDistance) * r2;
            Vector3 collideeSlideVec = collidee.GetRightDir() * collideeSlideSign * (1.0f / penDistance) * r1;
            Vector3 colliderMoveVec = colliderPushVec + colliderSlideVec;
            Vector3 collideeMoveVec = collideePushVec + collideeSlideVec;

            bool moveCollider = pushCollider || !pushCollidee;
            bool moveCollidee = pushCollidee || !pushCollider;

            if (moveCollider && manager.TestMoveRange(collider, collider.pos + colliderMoveVec, collider.pos + colliderMoveVec, true))
            {
                collider.pos = collider.pos + colliderMoveVec;
            }
            if (moveCollidee && manager.TestMoveRange(collidee, collidee.pos + collideeMoveVec, collidee.pos + collideeMoveVec, true))
            {
                collidee.pos = collidee.pos + collideeMoveVec;
            }
        }
    }
    public float GetRadius()
    {
        return param.unitSize * manager.GridSize / 2.0f;
    }
    private void HandleObjectCollision()
    {
        float radius = GetRadius();
        HandleUnitCollisions(this, currentSpeed, radius);
        if (manager.GetGridIndex(pos + currentVelocity) != manager.GetGridIndex(pos))
        {
            if (HandleStaticObjectCollision(this, this, radius, 0.0f, Vector3.zero, true, true))
            {
                ReRequestPath(false);
            }
        }
    }
    public void OwnerMoved(Vector3 oldPos, Vector3 oldForward)
    {
    }
    public void Update()
    {
        Vector3 oldPos = pos;
        Vector3 oldForward = forward;
        UpdateOwnerAccelAndHeading();
        UpdateOwnerPos(currentVelocity, this.forward * (currentSpeed + deltaSpeed));
        HandleObjectCollision();
        this.pos.y = 0.0f;
        OwnerMoved(oldPos, oldForward);
    }
    public void LateUpdate()
    {
    }
    public bool StartMoving(Vector3 goalPos, float goalRadius = 0.1f)
    {
        this.goalPos = new Vector3(goalPos.x, 0, goalPos.z);
        this.goalRadius = goalRadius;
        if ((this.goalPos - pos).sqrMagnitude < goalRadius * goalRadius)
        {
            return true;
        }
        atGoal = false;
        atEndOfPath = false;
        progressState = ProgressState.Active;
        numIdlingUpdates = 0;
        numIdlingSlowUpdates = 0;
        ReRequestPath(true);
        return true;
    }
    public void StopMoving(bool callScript, bool hardStop)
    {
        if (!atGoal)
        {
            float dist = BrakingDistance(currentSpeed, decRate);
            currWayPoint = pos + forward * dist;
            goalPos = currWayPoint;
        }
        StopEngine(callScript, hardStop);
        progressState = ProgressState.Done;
    }
    public bool IsBlockedOther(GridMoveAgent a)
    {
        if (this == a)
        {
            return false;
        }
        if (param.isPushResistant)
        {
            return true;
        }
        return param.teamID != a.param.teamID;
    }
}