using System;
using System.Collections.Generic;
using UnityEngine;

public enum GridMovePushFlags { EnemyResistant = 0x01, FriendResistant = 0x02 }

public struct GridMoveAgentParam
{
    public int teamID;
    public float mass;
    public float radius;
    public float maxSpeed;
    public float maxAcc;
    public float maxTurnAngle;
    public int pushFlags;
}

public class GridMoveAgent
{
    public enum State { Invalid, Walking }
    public enum MoveState { None, Failed, Valid, Requesting, WaitForQueue, WaitForPath }

    private bool isActive;
    private int state;
    private GridMoveAgentParam param;

}
public class GridNavAgent 
{ 
    public void Update(float deltaTime)
    {
    }
}

class GridNavNode
{
    private readonly int x;
    private readonly int z;
    private bool isBlocked;
    private List<GridNavAgent> agents;

    public int X { get => x; }
    public int Z { get => z; }
    public bool IsBlocked { get => isBlocked; set => isBlocked = value; }
    public List<GridNavAgent> Agents { get => agents; }

    public GridNavNode(int x, int z)
    {
        this.x = x;
        this.z = z;
        this.agents = new List<GridNavAgent>();
    }
}

public class GridNavManager
{
    private Vector3 bmin;
    private Vector3 bmax;
    private float nodeSize;
    private int xsize;
    private int zsize;
    private GridNavNode[] nodes;
    private List<GridNavAgent> agents;
    private GridPathFinder pathFinder;

    public float NodeSize { get => nodeSize; }
    public int XSize { get => xsize; }
    public int ZSize { get => zsize; }

    public bool Init(Vector3 bmin, Vector3 bmax, float nodeSize, int maxAgents)
    {
        if (bmin.x >= bmax.x || bmin.z >= bmax.z || nodeSize <= 0 || maxAgents <= 0)
        {
            return false;
        }
        this.bmin = bmin;
        this.bmax = bmax;
        this.nodeSize = nodeSize;
        this.xsize = Mathf.Max(1, (int)((bmax.x - bmin.x) / nodeSize));
        this.zsize = Mathf.Max(1, (int)((bmax.z - bmin.z) / nodeSize));
        nodes = new GridNavNode[xsize * zsize];
        for (int z = 0; z < zsize; z++)
        {
            for (int x = 0; x < xsize; x++)
            {
                nodes[x + z * xsize] = new GridNavNode(x, z);
            }
        }
        agents = new List<GridNavAgent>();
        pathFinder = new GridPathFinder(xsize, zsize);
        return true;
    }
    public void Clear()
    {
    }

    public void SetNodeBlocked(int x, int z, bool blocked)
    {
        if (x < 0 || x >= xsize || z < 0 || z >= zsize)
        {
            return;
        }
        nodes[x + z * xsize].IsBlocked = blocked;
    }
    public bool GetNodeXZUnclamped(Vector3 pos, out int x, out int z)
    {
        x = (int)((pos.x - bmin.x) / nodeSize);
        z = (int)((pos.z - bmin.z) / nodeSize);
        return x >= 0 && x < xsize && z >= 0 && z < zsize;
    }
    public void GetNodeXZ(Vector3 pos, out int x, out int z)
    {
        x = Mathf.Clamp((int)((pos.x - bmin.x) / nodeSize), 0, xsize - 1);
        z = Mathf.Clamp((int)((pos.z - bmin.z) / nodeSize), 0, zsize - 1);
    }
    public Vector3 GetNodePos(int x, int z)
    {
        Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
        return new Vector3(bmin.x + (x + 0.5f) * nodeSize, 0, bmin.z + (z + 0.5f) * nodeSize);
    }
    public void Update(float deltaTime)
    {
        foreach (var agent in agents)
        {
            agent.Update(deltaTime);
        }
    }
}