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

    private Vector3 goalPos;
    private float goalRadius;
    private bool atGoal;
    private bool atEndOfPath;

    private bool isWantRepath;
    private GridPath path;
    private Vector3 currWayPoint;
    private Vector3 nextWayPoint;
    private bool idling;
    private int numIdlingUpdates;
    private int numIdlingSlowUpdates;

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
        if ((pos - currWayPoint).magnitude > Mathf.Max(currentSpeed * 1.05f, manager.GridSize))
        {
            return false;
        }
        atEndOfPath = (currWayPoint - goalPos).sqrMagnitude <= goalRadius * goalRadius;
        return true;
    }
    private void SetNextWayPoint()
    {
        if (CanSetNextWayPoint())
        {
            currWayPoint = nextWayPoint;
            nextWayPoint = manager.NextWayPoint(this, this.path, currWayPoint, Mathf.Max(currentSpeed * 1.05f, 1.25f * manager.GridSize));
            //check nextwaypoint is success
        }
        if (manager.IsGridBlocked(this, currWayPoint) || manager.IsGridBlocked(this, nextWayPoint))
        {
            ReRequestPath(false);
        }
    }
    private void Arrived(bool call)
    {
        StopEngine(call, false);
    }
    private void Fail(bool call)
    {
        StopEngine(call, false);
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
            Vector3 wantedForward = forward;
            if (!atGoal)
            {
                wantedForward = (currWayPoint - pos).normalized;
            }
            wantedForward = GetObstacleAvoidanceDir(wantedForward);
            ChangeHeading(wantedForward);
            ChangeSpeed(maxSpeed);
        }
    }
    private void UpdateOwnerPos(Vector3 oldVelocity, Vector3 newVelocity)
    {
        if (newVelocity != Vector3.zero)
        {
            pos = pos + newVelocity;
            //TODO collision
        }
        currentVelocity = newVelocity;
        currentSpeed = newVelocity.magnitude;
        deltaSpeed = 0.0f;
    }
    private void HandleObjectCollision()
    {
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
        //goalRadius = Mathf.Max(goalRadius, param.unitSize * manager.GridSize / 2);
        this.goalPos = goalPos;
        this.goalRadius = goalRadius;
        atGoal = (goalPos - pos).sqrMagnitude < goalRadius * goalRadius;
        atEndOfPath = true;
        if (atGoal)
        {
            return true;
        }
        ReRequestPath(true);
        return true;
    }
    public void StopMoving()
    {
    }
    public bool IsBlockedOther(GridMoveAgent a)
    {
        return this != a && !param.isPushResistant && a.param.teamID == param.teamID;
    }
}