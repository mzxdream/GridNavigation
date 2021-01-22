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
    Vector3 oldPos = Vector3.zero;
    Vector3 oldLaterUpdatePos = Vector3.zero;
    Vector3 curVelocity = Vector3.zero;
    float curSpeed = 0.0f;
    float deltaSpeed = 0.0f;
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
        return OwnerMoved(h, pos - oldPos);
    }
    void UpdateOwnerAccelAndHeading()
    {
    }
    void UpdateOwnerPos(Vector3 oldSpeedVector, Vector3 newSpeedVector)
    {
    }
}