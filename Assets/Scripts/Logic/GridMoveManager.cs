using System.Collections.Generic;
using UnityEngine;

public class GridPath
{
    public List<Vector3> pathNodes = new List<Vector3>();
}

public class GridMoveManager
{
    int gameSpeed = 30;
    public int GameSpeed { get => gameSpeed; }
    int xszie = 11;
    int zsize = 11;
    float squareSize = 8.0f;
    public float SquareSize { get => squareSize; }
    public float WaypointRadius { get => squareSize * 1.25f; }
    int frameNum = 0;
    public int FrameNum { get => frameNum; }
    List<GridMoveAgent> agents = new List<GridMoveAgent>();
    int lastAgentID = 0;
    Dictionary<int, GridPath> paths = new Dictionary<int, GridPath>();
    int lastPathID = 0;

    public bool Init(int xsize, int zsize, float squareSize)
    {
        this.xszie = xsize;
        this.zsize = zsize;
        this.squareSize = squareSize;
        return true;
    }
    public void Clear()
    {
    }
    public GridMoveAgent AddAgent(Vector3 pos, Vector3 forward, GridMoveAgentParams agentParams)
    {
        var agent = new GridMoveAgent(++lastAgentID, this);
        if (!agent.Init(pos, forward, agentParams))
        {
            return null;
        }
        agents.Add(agent);
        return agent;
    }
    public void RemoveAgent(GridMoveAgent agent)
    {
        agent.Clear();
        agents.Remove(agent);
    }
    public void Update()
    {
        frameNum++;
        foreach (var agent in agents)
        {
            agent.Update();
        }
        foreach (var agent in agents)
        {
            agent.SlowUpdate();
        }
    }
    public int RequestPath(GridMoveAgent agent, Vector3 startPos, Vector3 goalPos, float goalRadius)
    {
        startPos = ClampInBounds(startPos);
        goalPos = ClampInBounds(goalPos);
        goalRadius = Mathf.Max(goalRadius, SquareSize * 2);

        GridPath path = new GridPath();
        path.pathNodes.Add(goalPos);
        path.pathNodes.Add(startPos);
        int pathID = ++lastPathID;
        paths.Add(pathID, path);
        return pathID;
    }
    public void DeletaPath(int pathID)
    {
    }
    public Vector3 NextWayPoint(GridMoveAgent agent, int pathID, Vector3 callerPos, float radius)
    {
        Vector3 waypoint = new Vector3(-1.0f, 0, -1.0f);
        if (paths.TryGetValue(pathID, out var path))
        {
            while (path.pathNodes.Count > 0)
            {
                waypoint = path.pathNodes[path.pathNodes.Count - 1];
                path.pathNodes.RemoveAt(path.pathNodes.Count - 1);
                if (GridMathUtils.SqrDistance2D(callerPos, waypoint) >= radius * radius)
                {
                    break;
                }
            }
        }
        return waypoint;
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
        int x = Mathf.Clamp((int)(pos.x / squareSize), 0, xszie - 1);
        int z = Mathf.Clamp((int)(pos.z / squareSize), 0, zsize - 1);
        return x + z * xszie;
    }
    public void OnSquareChange(GridMoveAgent agent, int oldMapSquare, int newMapSquare)
    {
        //todo
    }
    public List<GridMoveAgent> GetSolidsExact(Vector3 pos, float radius)
    {
        return agents;
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
        return false;
    }
    public bool SquareIsBlocked(GridMoveAgent collider, Vector3 pos)
    {
        return false;
    }
    public bool IsNonBlocking(GridMoveAgent collider, GridMoveAgent collidee)
    {
        return false;
    }
}