using UnityEngine;

public class Unit
{
    Vector3 forward = Vector3.forward;
    Vector3 waypointDir;
    Vector3 pos = Vector3.zero;
    Vector3 speed = Vector3.zero;
    float maxSpeed;
    float maxWantedSpeed;
    float radius = 0.5f;
    ProgressState progressState;
    bool wantRepath = false;
    Vector3 goalPos;
    float goalRadius;
    bool atGoal = false;
    bool atEndOfPath = false;
    int pathID = 0;
    float currentSpeed = 0f;
    float wantedSpeed = 0f;
    Vector3 currWayPoint;
    Vector3 nextWayPoint;
    float currWayPointDist = 0f;
    float prevWayPointDist = 0f;
    float turnRate = 10.0f;
    float accRate = 100f;
    float decRate = 100f;
    bool reversing = false;
    float deltaSpeed;

    Vector3 oldPos;
    Vector3 oldSlowUpdatePos;
    int mapSquare;
    int xsize;
    public int XSize { get => xsize; }
    int zsize;
    public int ZSize { get => zsize; }

    bool idling = false;
    int numIdlingUpdates = 0;
    int numIdlingSlowUpdates = 0;
    bool isPushResistant = false;
    float mass = 1.0f;
    int allyteam;
    bool isMoving = false;
    float maxSpeedDef;

    public Unit()
    {
        oldSlowUpdatePos = pos = Vector3.zero;
        mapSquare = Ground.Instance.GetSquare(oldSlowUpdatePos);
    }
    int GetNewPath()
    {
        if (MathUtils.SqrDistance2D(pos, goalPos) <= goalRadius * goalRadius)
        {
            return 0;
        }
        int newPathID = PathManager.Instance.RequestPath(this, pos, goalPos, goalRadius);
        if (newPathID != 0)
        {
            atGoal = false;
            atEndOfPath = false;

            currWayPoint = PathManager.Instance.NextWayPoint(this, newPathID, pos, Mathf.Max(0.5f, currentSpeed * 1.05f));
            nextWayPoint = PathManager.Instance.NextWayPoint(this, newPathID, currWayPoint, Mathf.Max(0.5f, currentSpeed * 1.05f));
        }
        else
        {
            StopEngine(false);
            progressState = ProgressState.Failed;
        }
        return newPathID;
    }
    void StopEngine(bool hardStop)
    {
        if (pathID != 0)
        {
            PathManager.Instance.DeletaPath(pathID);
            pathID = 0;
        }
        if (hardStop)
        {
            speed = Vector3.zero;
            currentSpeed = 0f;
        }
        wantedSpeed = 0f;
    }
    public void ReRequestPath(bool forceRequest)
    {
        if (forceRequest)
        {
            StopEngine(false);
            if (pathID == 0)
            {
                pathID = GetNewPath();
            }
            if (pathID != 0)
            {
                PathManager.Instance.UpdatePath(this, pathID);
            }
            wantRepath = false;
            return;
        }
        wantRepath = true;
    }
    public void StartMoving(Vector3 moveGoalPos, float moveGoalRadius = 0.001f)
    {
        goalPos = moveGoalPos;
        goalRadius = moveGoalRadius;

        atGoal = MathUtils.SqrDistance2D(pos, goalPos) <= (goalRadius * goalRadius);
        atEndOfPath = false;
        progressState = ProgressState.Active;
        currWayPointDist = 0f;
        prevWayPointDist = 0f;
        if (atGoal)
        {
            return;
        }
        ReRequestPath(true);
    }
    public Vector3 Here()
    {
        float time = currentSpeed / Mathf.Max(0.01f, decRate);
        float dist = decRate * time * time / 2.0f;
        return pos + forward * dist;
    }
    public void StopMoving(bool hardStop)
    {
        if (!atGoal)
        {
            goalPos = (currWayPoint = Here());
        }
        StopEngine(hardStop);
        progressState = ProgressState.Done;
    }
    void ChangeHeading(Vector3 newForward)
    {
        //TODO
        forward = newForward;
    }
    float GetDeltaSpeed(float targetSpeed, float curSpeed, float maxAccRate, float maxDecRate)
    {
        //float rawSpeedDiff = targetSpeed - curSpeed;
        //TODO
        return targetSpeed - curSpeed;
    }
    void ChangeSpeed(float newWantedSpeed)
    {
        wantedSpeed = newWantedSpeed;
        if (wantedSpeed <= 0.0f && currentSpeed < 0.01f)
        {
            currentSpeed = 0f;
            deltaSpeed = 0f;
            return;
        }
        var targetSpeed = maxSpeed;
        if (currWayPoint.y == -1.0f && nextWayPoint.y == -1.0f)//wait for new path
        {
            targetSpeed = 0f;
        }
        else
        {
            if (wantedSpeed > 0f)
            {
                //TODO check turn rate speed
                if (pathID == 0 && atEndOfPath) //wantToStop
                {
                    targetSpeed = 0f;
                }
                targetSpeed = Mathf.Min(targetSpeed, wantedSpeed);
            }
            else
            {
                targetSpeed = 0f;
            }
        }
        deltaSpeed = GetDeltaSpeed(targetSpeed, currentSpeed, accRate, decRate);
    }
    bool CanSetNextWayPoint()
    {
        if (pathID == 0)
        {
            return false;
        }
        if (currWayPoint.y != -1.0f && nextWayPoint.y != -1.0f)
        {
            float waypointDot = Vector3.Dot(forward, waypointDir);
            if (waypointDot >= 0.995f && currWayPointDist > Mathf.Max(Ground.Instance.GridSize, currentSpeed * 1.05f))
            {
                return false;
            }
            bool allowSkip = (currWayPoint - pos).sqrMagnitude <= Ground.Instance.GridSize * Ground.Instance.GridSize;
            if (!allowSkip)
            {
                bool rangeTest = Ground.Instance.TestMoveSquareRange(this, Vector3.Min(currWayPoint, pos), Vector3.Max(currWayPoint, pos), speed, true, true, true);
                if (!rangeTest)
                {
                    return false;
                }
            }
            atEndOfPath |= (currWayPoint - goalPos).sqrMagnitude <= goalRadius * goalRadius;
            if (atEndOfPath)
            {
                currWayPoint = goalPos;
                nextWayPoint = goalPos;
                return false;
            }
        }
        return true;
    }
    void Fail()
    {
        StopEngine(false);
        progressState = ProgressState.Failed;
    }
    void SetNextWayPoint()
    {
        if (CanSetNextWayPoint())
        {
            currWayPoint = nextWayPoint;
            nextWayPoint = PathManager.Instance.NextWayPoint(this, pathID, currWayPoint, Mathf.Max(0.5f, currentSpeed * 1.05f));
        }
        if (nextWayPoint.x == -1.0f && nextWayPoint.z == -1.0f)
        {
            Fail();
            return;
        }
        if (!Ground.Instance.SquareIsBlocked(this, currWayPoint) && !Ground.Instance.SquareIsBlocked(this, nextWayPoint))
        {
            return;
        }
        ReRequestPath(false);
    }
    bool WantToStop()
    {
        return pathID == 0 && atEndOfPath;
    }
    float CalcFootPrintMinExteriorRadius()
    {
        return Mathf.Sqrt(xsize * xsize + zsize * zsize) * 0.5f * SQUARE_SIZE;
    }
    float CalcFootPrintMaxExteriorRadius()
    {
        return Mathf.Max(xsize, zsize) * 0.5f * SQUARE_SIZE;
    }
    float CalcFootPrintAxisStretchFactor()
    {
        return Mathf.Abs(xsize - zsize) * 1.0f / (xsize + zsize);
    }
    bool IsMoving()
    {
        return isMoving;
    }
    static int CalcTurnSign(Unit a, Unit b)
    {
        //TODO
        return -1;
    }
    Vector3 GetRightDir()
    {
        return Vector3.Cross(forward, Vector3.up);
    }
    Vector3 GetObstacleAvoidanceDir(Vector3 desireDir)
    {
        if (WantToStop())
        {
            return forward;
        }
        var avoidanceVec = Vector3.zero;
        var avoidanceDir = desireDir;
        var lastAvoidanceDir = desireDir;
        var avoider = this;
        if (Vector3.Dot(avoider.forward, desireDir) < 0.0f)
        {
            return lastAvoidanceDir;
        }
        float avoidanceRadius = Mathf.Max(currentSpeed, 1.0f) * (avoider.radius * 2.0f);
        float avoiderRadius = avoider.CalcFootPrintMinExteriorRadius();
        foreach (var avoidee in Ground.Instance.GetSolids(avoider.pos, avoidanceRadius))
        {
            if (avoidee == avoider)
            {
                continue;
            }
            if (Ground.Instance.IsNonBlocking(avoider, avoidee))
            {
                continue;
            }
            bool avoideeMovable = !avoidee.isPushResistant;
            Vector3 avoideeVector = (avoider.pos + avoider.speed) - (avoidee.pos + avoidee.speed);
            float avoideeRadius = avoidee.CalcFootPrintMinExteriorRadius();
            float avoidanceRadiusSum = avoiderRadius + avoideeRadius;
            float avoidanceMassSum = avoider.mass + avoidee.mass;
            float avoideeMassScale = avoidee.mass / avoidanceMassSum;
            float avoideeDistSq = avoideeVector.sqrMagnitude;
            float avoideeDist = Mathf.Sqrt(avoideeDistSq) + 0.01f;
            if (avoideeMovable)
            {
                if (!avoidee.isMoving && avoidee.allyteam == avoider.allyteam)
                {
                    continue;
                }
            }
            float MAX_AVOIDEE_COSING = Mathf.Cos(120.0f * Mathf.Deg2Rad);
            if (Vector3.Dot(avoider.forward, -(avoideeVector / avoideeDist)) < MAX_AVOIDEE_COSING)
            {
                continue;
            }
            if (avoideeDistSq >= Mathf.Pow(Mathf.Max(currentSpeed, 1.0f) + avoidanceRadiusSum, 2))
            {
                continue;
            }
            if (avoideeDistSq >= (avoider.pos - goalPos).sqrMagnitude)
            {
                continue;
            }
            float avoiderTurnSign = -CalcTurnSign(avoider, avoidee);
            float avoideeTurnSign = -CalcTurnSign(avoidee, avoider);
            float avoidanceCosAngle = Mathf.Clamp(Vector3.Dot(avoider.forward, avoidee.forward), -1.0f, 1.0f);
            float avoidanceResponse = (1.0f - avoidanceCosAngle) + 0.1f;
            float avoidanceFallOff = (1.0f - Mathf.Min(1.0f, avoideeDist / (5.0f * avoidanceRadiusSum)));
            if (avoidanceCosAngle < 0.0f)
            {
                avoiderTurnSign = Mathf.Max(avoiderTurnSign, avoideeTurnSign);
            }
            avoidanceDir = avoider.GetRightDir() * avoiderTurnSign;
            avoidanceVec += (avoidanceDir * avoidanceResponse * avoidanceFallOff * avoideeMassScale);
        }
        avoidanceDir = Vector3.Lerp(desireDir, avoidanceVec, 0.5f).normalized;
        avoidanceDir = Vector3.Lerp(avoidanceDir, lastAvoidanceDir, 0.7f).normalized;
        return avoidanceDir;
    }
    void UpdateOwnerAccelAndHeading()
    {
        if (pathID == 0 && atEndOfPath)
        {
            currWayPoint.y = -1.0f;
            nextWayPoint.y = -1.0f;
            ChangeSpeed(0.0f);
        }
        else
        {
            Vector3 opos = pos;
            Vector3 ovel = speed;
            Vector3 ffd = forward;
            Vector3 cwp = currWayPoint;

            prevWayPointDist = currWayPointDist;
            currWayPointDist = MathUtils.Distance2D(currWayPoint, opos);

            float curGoalDistSq = MathUtils.SqrDistance2D(opos, goalPos);
            float minGoalDistSq = goalRadius * goalRadius;
            float spdGoalDistSq = (currentSpeed * 1.05f) * (currentSpeed * 1.05f);

            atGoal |= (curGoalDistSq <= minGoalDistSq);
            if (!reversing)
            {
                atGoal |= (curGoalDistSq <= spdGoalDistSq) && Vector3.Dot(ffd, goalPos - opos) > 0f && Vector3.Dot(ffd, goalPos - (opos + ovel)) <= 0f;
            }
            else
            {
                atGoal |= (curGoalDistSq <= spdGoalDistSq) && Vector3.Dot(ffd, goalPos - opos) < 0f && Vector3.Dot(ffd, goalPos - (opos + ovel)) > 0f;
            }
            if (!atGoal)
            {
                if (idling)
                {
                    numIdlingUpdates = Mathf.Max(360, numIdlingUpdates + 1);
                }
                else
                {
                    numIdlingUpdates = Mathf.Max(0, numIdlingUpdates - 1);
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
                    if (progressState == ProgressState.Active)
                    {
                        StopEngine(false);
                        progressState = ProgressState.Done;
                    }
                }
                else
                {
                    ReRequestPath(false);
                }
            }
            Vector3 waypointVec;
            if (MathUtils.SqrDistance2D(cwp, opos) > 1e-4f)
            {
                waypointVec = new Vector3(cwp.x - opos.x, 0, cwp.z = opos.z);
                waypointDir = waypointVec.normalized;
            }
            Vector3 modWantedDir = GetObstacleAvoidanceDir(atGoal ? ffd : waypointDir);
            ChangeHeading(modWantedDir);
            ChangeSpeed(maxWantedSpeed);
        }
    }
    void SetVelocityAndSpeed(Vector3 speed)
    {
    }
    void Move(Vector3 speed, bool relative)
    {
    }
    const int SQUARE_SIZE = 8;
    void UpdateOwnerPos(Vector3 oldSpeedVector, Vector3 newSpeedVector)
    {
        float oldSpeed = Vector3.Dot(oldSpeedVector, forward);
        float newSpeed = Vector3.Dot(newSpeedVector, forward);
        if (newSpeedVector != Vector3.zero)
        {
            SetVelocityAndSpeed(newSpeedVector);
            Move(speed, true);
            if (!Ground.Instance.TestMoveSquare(this, this.pos, this.speed, true, false, true))
            {
                bool updatePos = false;
                for (int n = 1; n <= 8; n++)
                {
                    updatePos = Ground.Instance.TestMoveSquare(this, this.pos + this.GetRightDir() * n, this.speed, true, false, true);
                    if (updatePos)
                    {
                        Move(pos + GetRightDir() * n, false);
                        break;
                    }
                    updatePos = Ground.Instance.TestMoveSquare(this, pos - GetRightDir() * n, speed, true, false, true);
                    if (updatePos)
                    {
                        Move(pos - GetRightDir() * n, false);
                        break;
                    }
                }
                if (!updatePos)
                {
                    Move(pos - newSpeedVector, false);
                }
            }
        }
        reversing = UpdateOwnerSpeed(Mathf.Abs(oldSpeed), Mathf.Abs(newSpeed), newSpeed);
    }
    bool UpdateOwnerSpeed(float oldSpeedAbs, float newSpeedAbs, float newSpeedRaw)
    {
        bool oldSpeedAbsGTZ = oldSpeedAbs > 0.01f;
        bool newSpeedAbsGTZ = newSpeedAbs > 0.01f;
        bool newSpeedRawLTZ = newSpeedRaw < 0.0f;
        isMoving = true;
        if (!oldSpeedAbsGTZ && newSpeedAbsGTZ)
        {
            //start moving callback
        }
        if (oldSpeedAbsGTZ && !newSpeedAbsGTZ)
        {
            //stop moving callback
        }
        currentSpeed = newSpeedAbs;
        deltaSpeed = 0.0f;
        return newSpeedRawLTZ;
    }
    static void HandleUnitCollisionsAux(Unit collider, Unit collidee)
    {
    }
    static void HandleUnitCollisions(Unit collider, float speed, float radius, float fpstretch)
    {
        bool allowUnitCollisionOverlap = true;
        bool allowCrushingAlliedUnits = false;
        bool allowPushingEnemyUnits = false;
        bool allowSepAxisCollisionTest = false;
        bool forceSepAxisCollisionTest = (fpstretch > 0.1f);

        foreach (var collidee in Ground.Instance.GetSolids(collider.pos, speed + radius * 2.0f))
        {
            if (collidee == collider)
            {
                continue;
            }
            if (Ground.Instance.IsNonBlocking(collider, collidee)
                || Ground.Instance.IsNonBlocking(collidee, collider))
            {
                continue;
            }
            float collideeSpeed = collidee.speed.magnitude;
            float collideeRadius = collidee.CalcFootPrintMaxExteriorRadius();
            Vector3 separationVec = collider.pos - collidee.pos;
            float separationRaids = (radius + collideeRadius) * (radius + collideeRadius);
            if (separationVec.sqrMagnitude - separationRaids > 0.01f)
            {
                continue;
            }
            HandleUnitCollisionsAux(collider, collidee);
            bool pushCollider = false; //todo
            bool pushCollidee = false; //todo
            if (!pushCollider && !pushCollidee)
            {
                bool allowNewPaht = !collider.atEndOfPath && !collider.atGoal;
                bool checkYardMap = pushCollider || pushCollidee;
                if (HandleStaticObjectCollision(collider, collidee, radius, collideeRadius, separationVec, allowNewPaht, checkYardMap, false))
                {
                    collider.ReRequestPath(false);
                }
                continue;
            }
            //TODO
            //float colliderRelRadius = radius / (radius + collideeRadius);
            //float collideeRelRadius = collideeRadius / (radius + collideeRadius);
            //float collisionRadiusSum = allowUnitCollisionOverlap ? ()
        }
    }
    static bool HandleStaticObjectCollision(Unit collider, Unit collidee, float colliderRadius, float collideeRadius, Vector3 separationVector, bool canRequestPath, bool checkYardMap, bool checkTerrain)
    {
        if (checkTerrain && !collidee.isMoving)
        {
            return false;
        }
        Vector3 pos = collider.pos;
        Vector3 vel = collider.speed;
        Vector3 rgt = collider.GetRightDir();

        Vector3 strafeVec = Vector3.zero;
        Vector3 bounceVec = Vector3.zero;
        Vector3 summedVec = Vector3.zero;

        if (checkYardMap || checkTerrain)
        {
            Vector3 sqrSumPosition = Vector3.zero;
            float sqrPenDistanceSum = 0.0f;
            float sqrPenDistanceCount = 0.0f;

            Vector3 rightDir2D = new Vector3(rgt.x, 0, rgt.z).normalized;
            Vector3 speedDir2D = new Vector3(vel.x, 0, vel.z).normalized;
            int xmid = (int)(pos.x + vel.x) / SQUARE_SIZE;
            int zmid = (int)(pos.z + vel.z) / SQUARE_SIZE;
            int xsh = collider.xsize / 2;
            int zsh = collider.zsize / 2;

            int xmin = Mathf.Min(-1, -xsh), xmax = Mathf.Max(1, xsh);
            int zmin = Mathf.Min(-1, -zsh), zmax = Mathf.Max(1, zsh);
            for (int z = zmin; z <= zmax; z++)
            {
                for (int x = xmin; x <= xmax; x++)
                {
                    int xabs = xmid + x;
                    int zabs = zmid + z;
                    if (checkTerrain && !Ground.Instance.SquareIsBlocked(collider, xabs, zabs))
                    {
                        continue;
                    }
                    Vector3 squarePos = new Vector3(xabs * SQUARE_SIZE + (SQUARE_SIZE / 2), pos.y, zabs * SQUARE_SIZE + (SQUARE_SIZE / 2));
                    Vector3 squareVec = pos - squarePos;
                    if (Vector3.Dot(squareVec, vel) > 0f)
                    {
                        continue;
                    }
                    float squareColRadiusSum = colliderRadius + Mathf.Sqrt(2 * (SQUARE_SIZE / 2) * (SQUARE_SIZE / 2));
                    float squareSepDistance = squareVec.magnitude + 0.1f;
                    float squarePenDistance = Mathf.Min(0, squareSepDistance - squareColRadiusSum);
                    bounceVec += (rightDir2D * (Vector3.Dot(rightDir2D, squareVec / squareSepDistance)));
                    sqrPenDistanceSum += squarePenDistance;
                    sqrPenDistanceCount += 1;
                    sqrSumPosition += new Vector3(squarePos.x, 0, squarePos.z);
                }
            }
            if (sqrPenDistanceCount > 0)
            {
                sqrSumPosition *= 1.0f / sqrPenDistanceCount;
                sqrPenDistanceSum *= 1.0f / sqrPenDistanceCount;

                float strafeSign = -MathUtils.Sign(Vector3.Dot(sqrSumPosition, rightDir2D) - Vector3.Dot(pos, rightDir2D));
                float bounceSign = MathUtils.Sign(Vector3.Dot(rightDir2D, bounceVec));
                float strafeScale = Mathf.Min(collider.maxSpeedDef, Mathf.Max(0.1f, -sqrPenDistanceSum * 0.5f));
                float bounceScale = Mathf.Min(collider.maxSpeedDef, Mathf.Max(0.1f, -sqrPenDistanceSum * 0.5f));

                strafeVec = rightDir2D * strafeSign;
                bounceVec = rightDir2D * bounceSign;
                summedVec = strafeVec + bounceVec;
                if (Ground.Instance.TestMoveSquare(collider, pos + summedVec, vel, checkTerrain, checkYardMap, checkTerrain))
                {
                    collider.Move(summedVec, true);
                    collider.currWayPoint += summedVec;
                    collider.nextWayPoint += summedVec;
                }
                else
                {
                    collider.Move((collider.oldPos - pos) + summedVec * 0.25f * (checkYardMap ? 1 : 0), true);
                }
            }
            return canRequestPath && summedVec != Vector3.zero;
        }
        {
            float colRadiusSum = colliderRadius + collideeRadius;
            float sepDistance = separationVector.magnitude + 0.1f;
            float penDistance = Mathf.Min(0.0f, sepDistance - colRadiusSum);
            float colSlideSign = -MathUtils.Sign(Vector3.Dot(collidee.pos, rgt) - Vector3.Dot(pos, rgt));

            float strafeScale = Mathf.Min(collider.currentSpeed, Mathf.Max(0.0f, -penDistance * 0.5f));
            float bounceScale = Mathf.Min(collider.currentSpeed, Mathf.Max(0.0f, -penDistance));

            strafeVec = rgt * colSlideSign * strafeScale;
            bounceVec = (separationVector / sepDistance) * bounceScale;
            summedVec = strafeVec + bounceVec;

            if (Ground.Instance.TestMoveSquare(collider, pos + summedVec, vel, true, true, true))
            {
                collider.Move(summedVec, true);
                collider.currWayPoint += summedVec;
                collider.nextWayPoint += summedVec;
            }
            else
            {
                collider.Move((collider.oldPos - pos) + summedVec * 0.25f * (Vector3.Dot(collider.forward, separationVector) < 0.25f ? 1 : 0), true);
            }
            return canRequestPath && penDistance < 0.0f;
        }
    }
    void HandleObjectCollisions()
    {
        var collider = this;
        float colliderFootPrintRadius = collider.CalcFootPrintMaxExteriorRadius();
        float colliderAxisStretchFact = collider.CalcFootPrintAxisStretchFactor();
        HandleUnitCollisions(collider, collider.speed.magnitude, colliderFootPrintRadius, colliderAxisStretchFact);
        bool squareChange = Ground.Instance.GetSquare(collider.pos + collider.speed) != Ground.Instance.GetSquare(collider.pos);
        if (!squareChange)
        {
            return;
        }
        if (!HandleStaticObjectCollision(collider, collider, colliderFootPrintRadius, 0.0f, Vector3.zero, true, false, true))
        {
            return;
        }
        ReRequestPath(false);
    }
    void AdjustPosToWaterLine()
    {
        pos.y = 0f;
    }
    void OwnerMoved(Vector3 oldForward, Vector3 posDiff)
    {
        if (posDiff.sqrMagnitude < 1e-4f)
        {
            speed = Vector3.zero;
            idling = true;
            idling &= (currWayPoint.y != -1.0f && nextWayPoint.y != -1.0f);
            idling &= Vector3.SqrMagnitude(oldForward - forward) < 1e-4f; //TODO
            return;
        }
        oldPos = pos;
        Vector3 ffd = forward * posDiff.sqrMagnitude / 2.0f;
        Vector3 wpd = waypointDir;
        idling = true;
        idling &= (currWayPointDist - prevWayPointDist) * (currWayPointDist - prevWayPointDist) < Vector3.Dot(ffd, wpd);
        idling &= (posDiff.sqrMagnitude < speed.sqrMagnitude / 4.0f);
    }
    public void Update()
    {
        Vector3 oldForward = forward;
        UpdateOwnerAccelAndHeading();
        UpdateOwnerPos(speed, forward * (speed.magnitude + deltaSpeed));
        HandleObjectCollisions();
        AdjustPosToWaterLine();
        OwnerMoved(oldForward, pos - oldPos);
    }
    public void LateUpdate()
    {
        if (progressState == ProgressState.Active)
        {
            if (pathID != 0)
            {
                if (idling)
                {
                    numIdlingSlowUpdates = Mathf.Min(16, numIdlingSlowUpdates + 1);
                }
                else
                {
                    numIdlingSlowUpdates = Mathf.Max(0, numIdlingSlowUpdates - 1);
                }
                if (numIdlingUpdates > 360.0f / turnRate)
                {
                    Debug.LogWarning("has path but failed");
                    if (numIdlingSlowUpdates < 16)
                    {
                        ReRequestPath(true);
                    }
                    else
                    {
                        StopEngine(false);
                        progressState = ProgressState.Failed;
                    }
                }
            }
            else
            {
                Debug.LogWarning("unit has no path");
                ReRequestPath(true);
            }
            if (wantRepath)
            {
                ReRequestPath(true);
            }
        }
        if (Ground.Instance.ClampInBounds(pos, out var newPos))
        {
            Debug.LogWarning("pos is clamp in bounds");
            pos = newPos;
        }
        if (pos != oldSlowUpdatePos)
        {
            oldSlowUpdatePos = pos;
            int newMapSquare = Ground.Instance.GetSquare(oldSlowUpdatePos);
            if (newMapSquare != mapSquare)
            {
                Ground.Instance.BlockingObjChange(this, mapSquare, newMapSquare);
                mapSquare = newMapSquare;
            }
        }
    }
}