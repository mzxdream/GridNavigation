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

    private bool isWantRepath;
    private bool atGoal;
    private bool atEndOfPath;
    private Vector3 goalPos;
    private float goalRadius;
    private GridPath path;
    private Vector3 currWayPoint;
    private Vector3 nextWayPoint;

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

        this.maxSpeed = param.maxSpeed / 30;
        this.accRate = Mathf.Max(0.01f, param.maxAcc);
        this.decRate = Mathf.Max(0.01f, param.maxDec);
        this.currentVelocity = Vector3.zero;
        this.currentSpeed = 0f;

        this.turnRate = 0f;
        this.turnAcc = 0f;

        this.isWantRepath = false;
        this.atGoal = true;
        this.atEndOfPath = true;
        this.goalPos = this.pos;
        this.goalRadius = 0.01f;

        return true;
    }
    public void Clear()
    {
    }
    private void ChangeHeading(Vector3 newWantedForward)
    {
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
            return;
        }
        float targetSpeed = maxSpeed;
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
        targetSpeed = Mathf.Min(targetSpeed, newWantedSpeed);
        float speedDiff = targetSpeed - currentSpeed;
        if (speedDiff > 0.0f)
        {
            deltaSpeed = Mathf.Min(speedDiff, accRate);
        }
        else
        {
            deltaSpeed = -Mathf.Min(-speedDiff, decRate);
        }
    }
    private void SetNextWayPoint()
    {
    }
    private void Arrived(bool call)
    {
    }
    private void ReRequestPath(bool forceRequest)
    {
    }
    private void UpdateOwnerAccelAndHeading()
    {
        if (WantToStop)
        {
            //ChangeHeading(heading);
            //ChangeSpeed(0.0f);
        }
        else
        {
            float curGoalDistSq = (pos - goalPos).sqrMagnitude;
            atGoal |= curGoalDistSq <= goalRadius * goalRadius;
            if (curGoalDistSq <= currentSpeed * 1.05f * currentSpeed * 1.05f)
            {
                if (!reversing)
                {
                    atGoal |= Vector3.Dot(forward, goalPos - pos) > 0.0f && Vector3.Dot(forward, goalPos - (pos + currentVelocity)) <= 0.0f;
                }
                else
                {
                    atGoal |= Vector3.Dot(forward, goalPos - pos) < 0.0f && Vector3.Dot(forward, goalPos - (pos + currentVelocity)) >= 0.0f;
                }
            }
            //TODO idling
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
            Vector3 wantedForward = forward;
            if (!atGoal)
            {
                wantedForward = (currWayPoint - pos).normalized;
            }
            //TODO obstacle
            ChangeHeading(wantedForward);
            ChangeSpeed(maxSpeed);
        }
    }
    public void Update()
    {
        if (isMoving)
        {
            bool atGoal = (goalPos - pos).sqrMagnitude <= goalRadius * goalRadius;
            if (atGoal)
            {
                isMoving = false;
                curSpeed = 0f;
            }
            else
            {
                if (path != null && (currWayPoint - pos).sqrMagnitude < (maxSpeed * 1.05f * maxSpeed * 1.05f))
                {
                    currWayPoint = nextWayPoint;
                    nextWayPoint = manager.NextWayPoint(this, this.path, currWayPoint, maxSpeed * 1.05f);
                }
                if (manager.IsGridBlocked(this, currWayPoint) || manager.IsGridBlocked(this, nextWayPoint))
                {
                    isWantRepath = true;
                }
                Vector3 waypointDir = (currWayPoint - pos).normalized;
                this.forward = waypointDir;
                curSpeed = maxSpeed;
            }
        }
        this.pos = this.pos + this.forward * curSpeed;
        //collision
        this.pos.y = 0.0f;
    }
    public void LateUpdate()
    {
    }
    public bool StartMoving(Vector3 goalPos, float goalRadius)
    {
        this.goalPos = goalPos;
        this.goalRadius = goalRadius;
        path = manager.FindPath(this, this.goalPos, this.goalRadius);
        if (path == null)
        {
            return false;
        }
        isMoving = true;
        isWantRepath = false;
        curSpeed = maxSpeed;
        currWayPoint = manager.NextWayPoint(this, this.path, pos, maxSpeed * 1.05f);
        nextWayPoint = manager.NextWayPoint(this, this.path, currWayPoint, maxSpeed * 1.05f);
        return true;
    }
    public void StopMoving()
    {
    }
    public bool IsBlockedOther(GridMoveAgent a)
    {
        return this != a && !isPushResistant && a.teamID == teamID;
    }
}