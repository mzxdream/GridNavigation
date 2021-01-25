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

    GridMoveManager manager;
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
    float curSpeed = 0.0f;
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
        curSpeed = 0.0f;
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
    bool WantToStop()
    {
        return pathID == 0 && atEndOfPath;
    }
    bool OwnerMoved(int oldHeading, Vector3 posDiff)
    {
        if (posDiff.sqrMagnitude < 1e-5f)
        {
            curVelocity = Vector3.zero;
            curSpeed = 0.0f;
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
        idling &= (posDiff.sqrMagnitude < (curSpeed * curSpeed * 0.25f));
        return true;
    }
    bool Update()
    {
        int h = heading;
        UpdateOwnerAccelAndHeading();
        Vector3 newVelocity = !reversing ? flatFrontDir * (curSpeed + deltaSpeed) : flatFrontDir * (-curSpeed + deltaSpeed);
        UpdateOwnerPos(curVelocity, newVelocity);
        HandleObjectCollisions();
        AdjustPosToWaterLine();
        return OwnerMoved(h, pos - oldPos);
    }
    void UpdateOwnerAccelAndHeading()
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
                float spdGoalDistSq = (curSpeed * 1.05f) * (curSpeed * 1.05f);

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
            ChangeHeading(GetHeadingFromVector(modWantedDir));
            ChangeSpeed(maxWantedSpeed);
        }
        return false;
    }
    void UpdateOwnerPos(Vector3 oldSpeedVector, Vector3 newSpeedVector)
    {
    }
    void HandleObjectCollisions()
    {
    }
    void AdjustPosToWaterLine()
    {
    }
}