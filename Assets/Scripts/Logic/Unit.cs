using UnityEngine;

public class Unit
{
    enum ProgressState { Done, Active, Failed };
    Vector3 forward = Vector3.forward;
    Vector3 pos = Vector3.zero;
    Vector3 speed = Vector3.zero;
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

    public Unit()
    {
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
        if (atGoal)
        {
            return;
        }
    }
    public void Update()
    {
    }
}