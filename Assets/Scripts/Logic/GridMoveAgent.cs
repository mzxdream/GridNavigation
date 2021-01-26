using UnityEngine;

public class GridMoveAgentParams
{
    public int xsize;
    public int zsize;
    public float speed;
    public float maxAcc;
    public float maxDec;
    public float turnRate;
    public bool isPushResistant;
}
public class GridMoveAgent
{
    enum ProgressState { Done, Active, Failed };
    const int MAX_HEADING = 32768;
    const int CIRCLE_DIVS = (MAX_HEADING << 1);
    const int NUM_HEADINGS = 4096;
    const int MAX_IDLING_SLOWUPDATES = 16;

    GridMoveManager manager;
    float mass = 1.0f;
    int xsize = 0;
    int zsize = 0;
    float minExteriorRadius = 0.0f;
    float maxInteriorRadius = 0.0f;
    bool isPushResistant = false;
    float maxSpeedDef = 0.0f;
    float accRate = 0.0f;
    float decRate = 0.0f;
    float turnRate = 0.0f;
    float turnAccel = 0.0f;

    Vector3 pos = Vector3.zero;
    Vector3 flatFrontDir = Vector3.forward;
    int heading = 0;
    ProgressState progressState = ProgressState.Done;
    int pathID = 0;
    Vector3 goalPos = Vector3.zero;
    float goalRadius = 0.0f;
    Vector3 oldPos = Vector3.zero;
    Vector3 oldLaterUpdatePos = Vector3.zero;
    Vector3 curVelocity = Vector3.zero;
    float currentSpeed = 0.0f;
    float deltaSpeed = 0.0f;
    float wantedSpeed = 0.0f;
    float maxSpeed = 0.0f;
    float maxWantedSpeed = 0.0f;
    Vector3 currWayPoint = Vector3.zero;
    Vector3 nextWayPoint = Vector3.zero;
    Vector3 lastAvoidanceDir = Vector3.zero;
    int wantedHeading = 0;
    bool idling = false;
    bool reversing = false;
    Vector3 waypointDir = Vector3.zero;
    float currWayPointDist = 0.0f;
    float prevWayPointDist = 0.0f;
    bool atEndOfPath = false;
    bool atGoal = false;
    int numIdlingUpdates = 0;
    int numIdlingSlowUpdates = 0;
    Vector3 oldSlowUpdatePos;
    int mapSquare;
    bool isMoving = false;
    bool wantRepath = false;
    //float turnSpeed = 0.0f;
    int nextObstacleAvoidanceFrame = 0;

    public GridMoveAgent(GridMoveManager manager)
    {
        this.manager = manager;
    }
    public bool Init(Vector3 position, Vector3 forward, GridMoveAgentParams agentParams)
    {
        pos = position;
        flatFrontDir = forward;
        xsize = agentParams.xsize;
        zsize = agentParams.zsize;
        minExteriorRadius = Mathf.Sqrt(xsize * xsize + zsize * zsize) * 0.5f * manager.SquareSize;
        maxInteriorRadius = Mathf.Max(xsize, zsize) * 0.5f * manager.SquareSize;
        isPushResistant = agentParams.isPushResistant;
        maxSpeedDef = agentParams.speed / manager.GameSpeed;
        accRate = Mathf.Max(0.01f, agentParams.maxAcc);
        decRate = Mathf.Max(0.01f, agentParams.maxDec);
        turnRate = Mathf.Clamp(agentParams.turnRate, 1.0f, CIRCLE_DIVS * 0.5f - 1.0f);
        turnAccel = turnRate * 0.333f;

        progressState = ProgressState.Done;
        pathID = 0;
        goalPos = pos;
        oldPos = pos;
        oldLaterUpdatePos = pos;
        curVelocity = Vector3.zero;
        currentSpeed = 0.0f;
        maxSpeed = maxSpeedDef;
        maxWantedSpeed = maxSpeedDef;
        currWayPoint = Vector3.zero;
        nextWayPoint = Vector3.zero;
        lastAvoidanceDir = Vector3.zero;
        wantedHeading = 0;
        idling = false;
        return true;
    }
    public void Clear()
    {
        if (pathID != 0)
        {
            manager.DeletaPath(pathID);
            pathID = 0;
        }
    }
    Vector3 GetRightDir()
    {
        return Vector3.Cross(flatFrontDir, Vector3.up);
    }
    float CalcFootPrintAxisStretchFactor()
    {
        return Mathf.Abs(xsize - zsize) * 1.0f / (xsize + zsize);
    }
    bool WantToStop()
    {
        return pathID == 0 && atEndOfPath;
    }
    bool OwnerMoved(int oldHeading, Vector3 posDiff)
    {
        if (posDiff.sqrMagnitude < 1e-5f)
        {
            curVelocity = Vector3.zero;
            currentSpeed = 0.0f;
            idling = true;
            idling &= (currWayPoint.y != -1.0f && nextWayPoint.y != -1.0f);
            idling &= (Mathf.Abs(heading - oldHeading) < turnRate);
            return false;
        }
        oldPos = pos;
        Vector3 ffd = flatFrontDir * posDiff.sqrMagnitude * 0.5f;
        Vector3 wpd = !reversing ? waypointDir : -waypointDir;
        idling = true;
        idling &= (Mathf.Abs(posDiff.y) < 1e-5f);
        idling &= ((currWayPointDist - prevWayPointDist) * (currWayPointDist - prevWayPointDist) < Vector3.Dot(ffd, wpd));
        idling &= (posDiff.sqrMagnitude < (currentSpeed * currentSpeed * 0.25f));
        return true;
    }
    bool Update()
    {
        int h = heading;
        UpdateOwnerAccelAndHeading();
        Vector3 newVelocity = !reversing ? flatFrontDir * (currentSpeed + deltaSpeed) : flatFrontDir * (-currentSpeed + deltaSpeed);
        UpdateOwnerPos(curVelocity, newVelocity);
        HandleObjectCollisions();
        AdjustPosToWaterLine();
        return OwnerMoved(h, pos - oldPos);
    }
    void SlowUpdate()
    {
        if (progressState == ProgressState.Active)
        {
            if (pathID != 0)
            {
                if (idling)
                {
                    numIdlingSlowUpdates = Mathf.Min(MAX_IDLING_SLOWUPDATES, numIdlingSlowUpdates + 1);
                }
                else
                {
                    numIdlingSlowUpdates = Mathf.Max(0, numIdlingSlowUpdates - 1);
                }
                if (numIdlingUpdates > MAX_HEADING / turnRate)
                {
                    Debug.LogWarning("has path but failed");
                    if (numIdlingSlowUpdates < MAX_IDLING_SLOWUPDATES)
                    {
                        ReRequestPath(true);
                    }
                    else
                    {
                        Fail(false);
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
        if (!manager.IsInBounds(pos))
        {
            oldPos = pos = manager.ClampInBounds(pos);
            Move(pos, false);
        }
        if (pos != oldSlowUpdatePos)
        {
            oldSlowUpdatePos = pos;
            int newMapSquare = manager.GetSquare(oldSlowUpdatePos);
            if (newMapSquare != mapSquare)
            {
                manager.OnSquareChange(this, mapSquare, newMapSquare);
                mapSquare = newMapSquare;
            }
        }
    }
    void StartMoving(Vector3 moveGoalPos, float moveGoalRadius)
    {
        goalPos = new Vector3(moveGoalPos.x, 0, moveGoalPos.z);
        goalRadius = moveGoalRadius;

        atGoal = MathUtils.SqrDistance2D(pos, goalPos) <= (goalRadius * goalRadius);
        atEndOfPath = false;

        progressState = ProgressState.Active;

        numIdlingUpdates = 0;
        numIdlingSlowUpdates = 0;

        currWayPointDist = 0f;
        prevWayPointDist = 0f;

        if (atGoal)
        {
            return;
        }
        ReRequestPath(true);
    }
    void StopMoving(bool callScript, bool hardStop)
    {
        if (!atGoal)
        {
            goalPos = currWayPoint = Here();
        }
        StopEngine(callScript, hardStop);
        progressState = ProgressState.Done;
    }
    bool UpdateOwnerAccelAndHeading() //FollowPath
    {
        if (WantToStop())
        {
            currWayPoint.y = -1.0f;
            nextWayPoint.y = -1.0f;
            ChangeHeading(heading);
            ChangeSpeed(0.0f);
        }
        else
        {
            Vector3 opos = pos;
            Vector3 ovel = curVelocity;
            Vector3 ffd = flatFrontDir;
            Vector3 cwp = currWayPoint;

            prevWayPointDist = currWayPointDist;
            currWayPointDist = MathUtils.Distance2D(currWayPoint, opos);
            {
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
                    atGoal |= (curGoalDistSq <= spdGoalDistSq) && Vector3.Dot(ffd, goalPos - opos) < 0f && Vector3.Dot(ffd, goalPos - (opos + ovel)) >= 0f;
                }
            }
            if (!atGoal)
            {
                if (!idling)
                {
                    numIdlingUpdates = Mathf.Max(0, numIdlingUpdates - 1);
                }
                else
                {
                    numIdlingUpdates = Mathf.Max(MAX_HEADING, numIdlingUpdates + 1);
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
            Vector3 waypointVec;
            if (MathUtils.SqrDistance2D(cwp, opos) > 1e-4f)
            {
                waypointVec = new Vector3(cwp.x - opos.x, 0, cwp.z = opos.z);
                waypointDir = waypointVec.normalized;
            }
            Vector3 modWantedDir = GetObstacleAvoidanceDir(atGoal ? ffd : waypointDir);
            ChangeHeading(MathUtils.GetHeadingFromVector(modWantedDir));
            ChangeSpeed(maxWantedSpeed);
        }
        return false;
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
                float curGoalDistSq = (pos - goalPos).sqrMagnitude;
                float minGoalTime = currentSpeed / Mathf.Max(0.001f, decRate);
                float minGoalDist = 0.5f * decRate * minGoalTime * minGoalTime;//1/2at^2
                float minGoalDistSq = minGoalDist * minGoalDist;
                Vector3 waypointDifFwd = waypointDir;
                Vector3 waypointDfRev = -waypointDifFwd;
                Vector3 waypointDif = !reversing ? waypointDifFwd : waypointDfRev;
                int turnDeltaHeading = (heading - MathUtils.GetHeadingFromVector(waypointDif));

                bool startBraking = curGoalDistSq <= minGoalDist;
                if (turnDeltaHeading != 0)
                {
                    float reqTurnAngle = Mathf.Abs(180.0f * (heading - wantedHeading) / MAX_HEADING);
                    float maxTurnAngle = (turnRate / CIRCLE_DIVS) * 360.0f;
                    float turnMaxSpeed = !reversing ? maxSpeed : 0.0f;
                    float turnModSpeed = turnMaxSpeed;
                    
                    if (reqTurnAngle != 0.0f)
                    {
                        turnModSpeed *= Mathf.Clamp(maxTurnAngle / reqTurnAngle, 0.1f, 1.0f);
                    }
                    if (waypointDir.sqrMagnitude > 0.1f)
                    {
                        //targetSpeed = targetSpeed;
                    }
                    if (atEndOfPath)
                    {
                        float absTurnSpeed = turnRate;
                        float framesToTurn = CIRCLE_DIVS / absTurnSpeed;
                        targetSpeed = Mathf.Min(targetSpeed, (currWayPointDist * Mathf.PI) / framesToTurn);
                    }
                }
                //wantedSpeed *= 1.0f;
                targetSpeed *= (!startBraking ? 1 : 0);
                targetSpeed *= (!WantToStop() ? 1 : 0);
                targetSpeed = Mathf.Min(targetSpeed, wantedSpeed);
            }
            else
            {
                targetSpeed = 0f;
            }
        }
        deltaSpeed = GetDeltaSpeed(targetSpeed, currentSpeed, accRate, decRate, reversing);
    }
    void ChangeHeading(int newHeading)
    {
        wantedHeading = newHeading;
        int rawDeltaHeading = GetDeltaHeading(wantedHeading, heading, turnRate);
        //TODO callback
        heading += rawDeltaHeading;
        flatFrontDir = MathUtils.GetVectorFromHeading(heading);
    }
    Vector3 GetObstacleAvoidanceDir(Vector3 desireDir)
    {
        if (WantToStop())
        {
            return flatFrontDir;
        }

        var avoidanceVec = Vector3.zero;
        var avoidanceDir = desireDir;

        var lastAvoidanceDir = desireDir;

        var avoider = this;
        if (Vector3.Dot(avoider.flatFrontDir, desireDir) < 0.0f)
        {
            return lastAvoidanceDir;
        }

        float avoidanceRadius = Mathf.Max(currentSpeed, 1.0f) * (avoider.minExteriorRadius * 2.0f);
        float avoiderRadius = avoider.minExteriorRadius;
        foreach (var avoidee in manager.GetSolidsExact(avoider.pos, avoidanceRadius))
        {
            if (avoidee == avoider)
            {
                continue;
            }
            bool avoideeMovable = !avoidee.isPushResistant;

            Vector3 avoideeVector = (avoider.pos + avoider.curVelocity) - (avoidee.pos + avoidee.curVelocity);

            float avoideeRadius = avoidee.minExteriorRadius;
            float avoidanceRadiusSum = avoiderRadius + avoideeRadius;
            float avoidanceMassSum = avoider.mass + avoidee.mass;
            float avoideeMassScale = avoidee.mass / avoidanceMassSum;
            float avoideeDistSq = avoideeVector.sqrMagnitude;
            float avoideeDist = Mathf.Sqrt(avoideeDistSq) + 0.01f;

            if (avoideeMovable)
            {
                //TODO
                //if (!avoidee.isMoving && avoidee.allyteam == avoider.allyteam)
                //{
                //    continue;
                //}
            }
            float MAX_AVOIDEE_COSING = Mathf.Cos(120.0f * Mathf.Deg2Rad);
            if (Vector3.Dot(avoider.flatFrontDir, -(avoideeVector / avoideeDist)) < MAX_AVOIDEE_COSING)
            {
                continue;
            }
            var t = Mathf.Max(currentSpeed, 1.0f) * manager.GameSpeed + avoidanceRadiusSum;
            if (avoideeDistSq >= t * t)
            {
                continue;
            }
            if (avoideeDistSq >= MathUtils.SqrDistance2D(avoider.pos, goalPos))
            {
                continue;
            }

            float avoiderTurnSign = -MathUtils.Sign(Vector3.Dot(avoidee.pos, avoider.GetRightDir()) - Vector3.Dot(avoider.pos, avoider.GetRightDir()));
            float avoideeTurnSign = -MathUtils.Sign(Vector3.Dot(avoider.pos, avoidee.GetRightDir()) - Vector3.Dot(avoidee.pos, avoidee.GetRightDir()));

            float avoidanceCosAngle = Mathf.Clamp(Vector3.Dot(avoider.flatFrontDir, avoidee.flatFrontDir), -1.0f, 1.0f);
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
    int GetNewPath()
    {
        if (MathUtils.SqrDistance2D(pos, goalPos) <= goalRadius * goalRadius)
        {
            return 0;
        }
        int newPathID = manager.RequestPath(this, pos, goalPos, goalRadius);
        if (newPathID != 0)
        {
            atGoal = false;
            atEndOfPath = false;

            currWayPoint = manager.NextWayPoint(this, newPathID, pos, Mathf.Max(manager.WaypointRadius, currentSpeed * 1.05f));
            nextWayPoint = manager.NextWayPoint(this, newPathID, currWayPoint, Mathf.Max(manager.WaypointRadius, currentSpeed * 1.05f));
        }
        else
        {
            Fail(false);
        }
        return newPathID;
    }
    void ReRequestPath(bool forceRequest)
    {
        if (forceRequest)
        {
            StopEngine(false, false);
            StartEngine(false);
            wantRepath = false;
            return;
        }
        wantRepath = true;
    }
    bool CanSetNextWayPoint()
    {
        if (pathID == 0)
        {
            return false;
        }
        if (currWayPoint.y != -1.0f && nextWayPoint.y != -1.0f)
        {
            int dirSign = !reversing ? 1 : -1;
            //float absTurnSpeed = Mathf.Max(0.0001f, Mathf.Abs(turnSpeed));
            float absTurnSpeed = turnRate;
            float framesToTurn = CIRCLE_DIVS / absTurnSpeed;

            float turnRadius = Mathf.Max((currentSpeed * framesToTurn) / (2.0f * Mathf.PI), currentSpeed * 1.05f);
            float waypointDot = Mathf.Clamp(Vector3.Dot(waypointDir, flatFrontDir * dirSign), -1.0f, 1.0f);

            if (currWayPointDist > turnRadius * 2.0f)
            {
                return false;
            }
            if (currWayPointDist > Mathf.Max(manager.SquareSize * 1.0f, currentSpeed * 1.05f) && waypointDot >= 0.995f)
            {
                return false;
            }
            {
                bool rangeTest = manager.TestMoveSquareRange(this, Vector3.Min(currWayPoint, pos), Vector3.Max(currWayPoint, pos), curVelocity, true, true, true);
                bool allowSkip = (currWayPoint - pos).sqrMagnitude <= manager.SquareSize * manager.SquareSize;
                if (!allowSkip && !rangeTest)
                {
                    return false;
                }
            }
            {
                atEndOfPath |= (currWayPoint - goalPos).sqrMagnitude <= goalRadius * goalRadius;
            }
            if (atEndOfPath)
            {
                currWayPoint = goalPos;
                nextWayPoint = goalPos;
                return false;
            }
        }
        return true;
    }
    void SetNextWayPoint()
    {
        if (CanSetNextWayPoint())
        {
            currWayPoint = nextWayPoint;
            nextWayPoint = manager.NextWayPoint(this, pathID, currWayPoint, Mathf.Max(manager.WaypointRadius, currentSpeed * 1.05f));
        }
        if (nextWayPoint.x == -1.0f && nextWayPoint.z == -1.0f)
        {
            Fail(false);
            return;
        }
        if (!manager.SquareIsBlocked(this, currWayPoint) && !manager.SquareIsBlocked(this, nextWayPoint))
        {
            return;
        }
        ReRequestPath(false);
    }
    Vector3 Here()
    {
        float time = currentSpeed / Mathf.Max(0.01f, decRate);
        float dist = 0.5f * decRate * time * time;
        int sign = !reversing ? 1 : -1;

        Vector3 pos2D = new Vector3(pos.x, 0, pos.z);
        Vector3 dir2D = flatFrontDir * dist * sign;
        return pos + dir2D;
    }
    void StartEngine(bool callScript)
    {
        if (pathID == 0)
        {
            pathID = GetNewPath();
        }
        if (pathID != 0)
        {
            if (callScript)
            {
                //TODO
            }
        }
        nextObstacleAvoidanceFrame = manager.FrameNum;
    }
    void StopEngine(bool callScript, bool hardStop)
    {
        if (pathID != 0)
        {
            manager.DeletaPath(pathID);
            pathID = 0;
            if (callScript)
            {
                //TODO
            }
        }
        if (hardStop)
        {
            SetVelocityAndSpeed(Vector3.zero);
            currentSpeed = 0f;
        }
        wantedSpeed = 0f;
    }
    void Arrived(bool callScript)
    {
        if (progressState == ProgressState.Active)
        {
            StopEngine(callScript, false);
            progressState = ProgressState.Done;
        }
    }
    void Fail(bool callScript)
    {
        StopEngine(callScript, false);
        progressState = ProgressState.Failed;
    }
    void HandleObjectCollisions()
    {
        var collider = this;
        float colliderFootPrintRadius = collider.maxInteriorRadius;
        float colliderAxisStretchFact = collider.CalcFootPrintAxisStretchFactor();
        HandleUnitCollisions(collider, collider.currentSpeed, colliderFootPrintRadius, colliderAxisStretchFact);

        bool squareChange = manager.GetSquare(collider.pos + collider.curVelocity) != manager.GetSquare(collider.pos);
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
    static bool HandleStaticObjectCollision(GridMoveAgent collider, GridMoveAgent collidee, float colliderRadius, float collideeRadius
        , Vector3 separationVector, bool canRequestPath, bool checkYardMap, bool checkTerrain)
    {
        if (checkTerrain && !collider.isMoving)
        {
            return false;
        }

        var manager = collider.manager;

        Vector3 pos = collider.pos;
        Vector3 vel = collider.curVelocity;
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

            int xmid = (int)((pos.x + vel.x) / manager.SquareSize);
            int zmid = (int)((pos.z + vel.z) / manager.SquareSize);

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

                    if (checkTerrain)
                    {
                        //TODO check pos speed mod
                        continue;
                    }
                    if (!checkTerrain && !manager.SquareIsBlocked(collider, xabs, zabs))
                    {
                        continue;
                    }
                    Vector3 squarePos = new Vector3(xabs * manager.SquareSize + (manager.SquareSize / 2), pos.y, zabs * manager.SquareSize + (manager.SquareSize / 2));
                    Vector3 squareVec = pos - squarePos;
                    if (Vector3.Dot(squareVec, vel) > 0f)
                    {
                        continue;
                    }
                    float squareColRadiusSum = colliderRadius + Mathf.Sqrt(2 * (manager.SquareSize / 2) * (manager.SquareSize / 2));
                    float squareSepDistance = squareVec.magnitude + 0.1f;
                    float squarePenDistance = Mathf.Min(0.0f, squareSepDistance - squareColRadiusSum);

                    bounceVec += (rightDir2D * (Vector3.Dot(rightDir2D, squareVec / squareSepDistance)));

                    sqrPenDistanceSum += squarePenDistance;
                    sqrPenDistanceCount += 1.0f;
                    sqrSumPosition += new Vector3(squarePos.x, 0, squarePos.z);
                }
            }
            if (sqrPenDistanceCount > 0.0f)
            {
                sqrSumPosition *= (1.0f / sqrPenDistanceCount);
                sqrPenDistanceSum *= (1.0f / sqrPenDistanceCount);
                sqrPenDistanceCount *= (1.0f / sqrPenDistanceCount);

                float strafeSign = -MathUtils.Sign(Vector3.Dot(sqrSumPosition, rightDir2D) - Vector3.Dot(pos, rightDir2D));
                float bounceSign = MathUtils.Sign(Vector3.Dot(rightDir2D, bounceVec));
                float strafeScale = Mathf.Min(collider.maxSpeedDef, Mathf.Max(0.1f, -sqrPenDistanceSum * 0.5f));
                float bounceScale = Mathf.Min(collider.maxSpeedDef, Mathf.Max(0.1f, -sqrPenDistanceSum * 0.5f));

                strafeVec = rightDir2D * strafeSign * strafeScale;
                bounceVec = rightDir2D * bounceSign * bounceScale;
                summedVec = strafeVec + bounceVec;
                if (manager.TestMoveSquare(collider, pos + summedVec, vel, checkTerrain, checkYardMap, checkTerrain))
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

            if (manager.TestMoveSquare(collider, pos + summedVec, vel, true, true, true))
            {
                collider.Move(summedVec, true);
                collider.currWayPoint += summedVec;
                collider.nextWayPoint += summedVec;
            }
            else
            {
                collider.Move((collider.oldPos - pos) + summedVec * 0.25f * (Vector3.Dot(collider.flatFrontDir, separationVector) < 0.25f ? 1 : 0), true);
            }
            return canRequestPath && penDistance < 0.0f;
        }
    }
    static void HandleUnitCollisions(GridMoveAgent collider, float speed, float radius, float fpstretch)
    {
        var manager = collider.manager;

        bool allowUnitCollisionOverlap = true;
        bool allowCrushingAlliedUnits = false;
        bool allowPushingEnemyUnits = false;
        bool allowSepAxisCollisionTest = false;
        bool forceSepAxisCollisionTest = (fpstretch > 0.1f);

        foreach (var collidee in manager.GetSolidsExact(collider.pos, speed + radius * 2.0f))
        {
            if (collidee == collider)
            {
                continue;
            }
            if (manager.IsNonBlocking(collider, collidee))
            {
                continue;
            }
            float collideeSpeed = collidee.currentSpeed;
            float collideeRadius = collidee.maxInteriorRadius;

            Vector3 separationVec = collider.pos - collidee.pos;
            float separationRadius = (radius + collideeRadius) * (radius + collideeRadius);

            if (separationVec.sqrMagnitude - separationRadius > 0.01f)
            {
                continue;
            }
            bool pushCollider = true;
            bool pushCollidee = true;
            bool alliedCollision = false; //TODO
            bool collideeYields = collider.isMoving && !collidee.isMoving;
            bool ignoreCollidee = collideeYields && alliedCollision;

            HandleUnitCollisionsAux(collider, collidee);

            pushCollider = pushCollider && alliedCollision && !collider.isPushResistant;
            pushCollidee = pushCollidee && alliedCollision && !collidee.isPushResistant;

            if (!pushCollider && !pushCollidee)
            {
                bool allowNewPath = !collider.atEndOfPath && !collider.atGoal;
                bool checkYardMap = pushCollider || pushCollidee;
                if (HandleStaticObjectCollision(collider, collidee, radius, collideeRadius, separationVec, allowNewPath, checkYardMap, false))
                {
                    collider.ReRequestPath(false);
                }
                continue;
            }
            //TODO
        }
    }
    void AdjustPosToWaterLine()
    {
        pos.y = 0.0f;
    }
    void UpdateOwnerPos(Vector3 oldSpeedVector, Vector3 newSpeedVector)
    {
    }
}