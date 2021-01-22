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
    Vector3 pos = Vector3.zero;
    Vector3 flatFrontDir = Vector3.forward;
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

    ProgressState progressState = ProgressState.Done;
    Vector3 goalPos = Vector3.zero;
    Vector3 oldPos = Vector3.zero;
    Vector3 oldLaterUpdatePos = Vector3.zero;
    float maxSpeed = 0.0f;
    float maxWantedSpeed = 0.0f;
    Vector3 currWayPoint = Vector3.zero;
    Vector3 nextWayPoint = Vector3.zero;
    Vector3 lastAvoidanceDir = Vector3.zero;
    int wantedHeading = 0;

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
        goalPos = pos;
        oldPos = pos;
        oldLaterUpdatePos = pos;
        maxSpeed = maxSpeedDef;
        maxWantedSpeed = maxSpeedDef;
        currWayPoint = Vector3.zero;
        nextWayPoint = Vector3.zero;
        lastAvoidanceDir = Vector3.zero;
        wantedHeading = 0;
        return true;
    }
    public void Clear()
    {
    }
}