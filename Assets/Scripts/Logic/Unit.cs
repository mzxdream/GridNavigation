using UnityEngine;

public class Unit
{
    enum ProgressState { Done, Active, Failed };
    Vector3 forward = Vector3.forward;
    Vector3 waypointDir;
    Vector3 pos = Vector3.zero;
    Vector3 speed = Vector3.zero;
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
    void ChangeSpeed(float newWantedSpeed)
    {
    }
    void SetNextWayPoint()
    {
    }
    Vector3 GetObstacleAvoidanceDir(Vector3 desireDir)
    {
        return Vector3.zero;
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
    void UpdateOwnerPos(Vector3 oldSpeed, Vector3 newSpeed)
    {

    }
    void HandleObjectCollisions()
    {
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