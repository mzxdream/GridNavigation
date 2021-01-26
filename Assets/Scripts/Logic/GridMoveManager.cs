using System.Collections.Generic;
using UnityEngine;

public class GridMoveManager
{
    int gameSpeed = 30;
    public int GameSpeed { get => gameSpeed; }
    float squareSize = 8.0f;
    public float SquareSize { get => squareSize; }
    public float WaypointRadius { get => squareSize * 1.25f; }
    int frameNum = 0;
    public int FrameNum { get => frameNum; }

    public bool Init()
    {
        return true;
    }
    public void Clear()
    {
    }
    public GridMoveAgent AddAgent(Vector3 pos, Vector3 forward, GridMoveAgentParams agentParams)
    {
        var agent = new GridMoveAgent(this);
        if (!agent.Init(pos, forward, agentParams))
        {
            return null;
        }
        return agent;
    }
    public void RemoveAgent(GridMoveAgent agent)
    {
    }
    public void Update()
    {
    }
    public int RequestPath(GridMoveAgent agent, Vector3 startPos, Vector3 goalPos, float goalRadius)
    {
        return 0;
    }
    public void DeletaPath(int pathID)
    {
    }
    public Vector3 NextWayPoint(GridMoveAgent agent, int pathID, Vector3 callerPos, float radius)
    {
        return Vector3.zero;
    }
    public void TerrainChange(int xmin, int zmin, int xmax, int zmax)
    {
    }
    public bool IsInBounds(Vector3 pos)
    {
        return true;
    }
    public Vector3 ClampInBounds(Vector3 pos)
    {
        return pos;
    }
    public int GetSquare(Vector3 pos)
    {
        return 0;
    }
    public void OnSquareChange(GridMoveAgent agent, int oldMapSquare, int newMapSquare)
    {
        //todo
    }
    public List<GridMoveAgent> GetSolidsExact(Vector3 pos, float radius)
    {
        return null;
    }
    public bool TestMoveSquareRange(GridMoveAgent collider, Vector3 rangeMins, Vector3 rangeMaxs, Vector3 testMoveDir, bool testTerrain, bool testObjects, bool centerOnly)
    {
        return true;
    }
    public bool TestMoveSquare(GridMoveAgent collider, Vector3 testMovePos, Vector3 testMoveDir, bool testTerrain, bool testObjectes, bool centerOnly)
    {
        return true;
    }
    public bool SquareIsBlocked(GridMoveAgent collider, int xSquare, int zSquare)
    {
        return true;
    }
    public bool SquareIsBlocked(GridMoveAgent collider, Vector3 pos)
    {
        return true;
    }
}