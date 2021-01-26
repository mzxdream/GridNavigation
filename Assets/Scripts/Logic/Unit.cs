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
    //ProgressState progressState;
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

    /*
    public Unit()
    {
        oldSlowUpdatePos = pos = Vector3.zero;
        mapSquare = Ground.Instance.GetSquare(oldSlowUpdatePos);
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

 
    void Fail()
    {
        StopEngine(false);
        progressState = ProgressState.Failed;
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
    void UpdateOwnerAccelAndHeading()
    {
       
    }
    void SetVelocityAndSpeed(Vector3 speed)
    {
    }
    void Move(Vector3 speed, bool relative)
    {
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
    static 
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
    */
}