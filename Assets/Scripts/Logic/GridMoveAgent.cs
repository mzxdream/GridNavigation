using UnityEngine;

public class GridMoveAgentParams
{
    int xsize;
    int zsize;
    public float speed;
    public float maxAcc;
    public float maxDec;
    public bool isPushResistant;
}
public class GridMoveAgent
{
    enum ProgressState { Done, Active, Failed };

    GridMoveManager manager;
    Vector3 pos = Vector3.zero;
    Vector3 flatFrontDir = Vector3.forward;
    int xsize = 0;
    int zsize = 0;
    float minExteriorRadius = 0.0f;
    float maxInteriorRadius = 0.0f;
    bool isPushResistant = false;
    float maxSpeedDef = 0.0f;

    ProgressState progressState = ProgressState.Done;
    Vector3 oldPos = Vector3.zero;
    Vector3 oldLaterUpdatePos = Vector3.zero;
    Vector3 goalPos = Vector3.zero;
    float maxSpeed = 0.0f;
    float maxWantedSpeed = 0.0f;

    Vector3 currWayPoint = Vector3.zero;
    Vector3 nextWayPoint = Vector3.zero;
    Vector3 lastAvoidanceDir = Vector3.zero;
    int wantedHeading = 0;

    float trunRate = 0.0f;
    float turnAccel = 0.0f;
    float accRate = 0.0f;
    float decRate = 0.0f;


    public GridMoveAgent(GridMoveManager manager)
    {
        this.manager = manager;
    }
    public bool Init(Vector3 pos, GridMoveAgentParams agentParams)
    {
        this.agentParams = agentParams;
        this.pos = pos;
        this.oldPos = this.pos;
        this.oldLaterUpdatePos = this.pos;
        this.goalPos = this.pos;
        this.maxSpeedDef = agentParams.speed / manager.GameSpeed;
        this.maxSpeed = this.maxSpeedDef;
        this.maxWantedSpeed = this.maxSpeedDef;
        return true;
    }
    public void Clear()
    {
    }
}