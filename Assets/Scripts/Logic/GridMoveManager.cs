using System;
using System.Collections.Generic;
using UnityEngine;

class GridTile
{
    public int index;
    public bool isBlocked;
    public List<GridMoveAgent> agents;
}

public class GridPath
{
    public List<Vector3> positions;
}

public class GridMoveManager
{
    private Vector3 bmin;
    private Vector3 bmax;
    private int xsize;
    private int zsize;
    private float tileSize;
    private float pixelSize;
    private int gameSpeed;

    private GridTile[] tiles;
    private List<GridMoveAgent> agents;
    private GridPathFinder pathFinder;

    public int XSize { get => xsize; }
    public int ZSize { get => zsize; }
    public float TileSize { get => tileSize; }
    public float PixelSize { get => pixelSize; }
    public int TilePerPixel { get => 8; }
    public int GameSpeed { get => gameSpeed; }

    public bool Init(Vector3 bmin, Vector3 bmax, float tileSize, int gameSpeed, int maxAgents)
    {
        this.bmin = bmin;
        this.bmax = bmax;
        xsize = (int)((bmax.x - bmin.x) / tileSize);
        zsize = (int)((bmax.z - bmin.z) / tileSize);
        this.tileSize = tileSize;
        this.pixelSize = tileSize / TilePerPixel;
        this.gameSpeed = gameSpeed;

        tiles = new GridTile[xsize * zsize];
        for (int z = 0; z < zsize; z++)
        {
            for (int x = 0; x < xsize; x++)
            {
                var index = x + z * xsize;
                tiles[index] = new GridTile { index = index, isBlocked = false, agents = new List<GridMoveAgent>() };
            }
        }
        agents = new List<GridMoveAgent>();
        pathFinder = new GridPathFinder(xsize, zsize);
        return true;
    }
    public void Clear()
    {
    }
    public void Update()
    {
        foreach (var agent in agents)
        {
            agent.Update();
        }
    }
    public void LateUpdate()
    {
        foreach (var agent in agents)
        {
            agent.LateUpdate();
        }
    }
    public GridMoveAgent CreateAgent(Vector3 pos, Vector3 forward, GridMoveAgentParam agentParam)
    {
        GridMoveAgent agent = new GridMoveAgent(this);
        if (!agent.Init(agents.Count + 1, pos, forward, agentParam))
        {
            return null;
        }
        agents.Add(agent);
        return agent;
    }
    public Vector3 ClampInBounds(Vector3 pos)
    {
        return new Vector3(Mathf.Clamp(pos.x, bmin.x, bmax.x), pos.y, Mathf.Clamp(pos.z, bmin.z, bmax.z));
    }
    public bool GetTileXZUnclamped(Vector3 pos, out int x, out int z)
    {
        x = (int)((pos.x - bmin.x) / tileSize);
        z = (int)((pos.z - bmin.z) / tileSize);
        return x >= 0 && x < xsize && z >= 0 && z < zsize;
    }
    public void GetTileXZ(Vector3 pos, out int x, out int z)
    {
        GetTileXZUnclamped(pos, out x, out z);
        x = Mathf.Clamp(x, 0, xsize - 1);
        z = Mathf.Clamp(z, 0, zsize - 1);
    }
    public int GetTileIndex(Vector3 pos)
    {
        GetTileXZ(pos, out int x, out int z);
        return x + z * xsize;
    }
    public Vector3 GetTilePos(int x, int z)
    {
        Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
        return new Vector3(bmin.x + (x + 0.5f) * tileSize, 0, bmin.z + (z + 0.5f) * tileSize);
    }
    public Vector3 GetTilePos(int index)
    {
        Debug.Assert(index >= 0 && index < xsize * zsize);
        int z = index / xsize;
        int x = index - z * xsize;
        return new Vector3(bmin.x + (x + 0.5f) * tileSize, 0, bmin.z + (z + 0.5f) * tileSize);
    }
    public bool IsTileCenterBlocked(GridMoveAgent agent, int x, int z, bool checkAgents)
    {
        if (x < 0 || x >= xsize || z < 0 || z >= zsize)
        {
            return true;
        }
        var tile = tiles[x + z * xsize];
        if (tile.isBlocked)
        {
            return true;
        }
        if (checkAgents)
        {
            foreach (var a in tile.agents)
            {
                if (a.IsBlocked(agent))
                {
                    return true;
                }
            }
        }
        return false;
    }
    public bool IsTileBlocked(GridMoveAgent agent, int x, int z, bool checkAgents)
    {
        int offset = agent.UnitSize / 2;
        int xmin = x - offset, xmax = x + offset;
        int zmin = z - offset, zmax = z + offset;
        for (int j = zmin; j <= zmax; j++)
        {
            for (int i = xmin; i <= xmax; i++)
            {
                if (IsTileCenterBlocked(agent, i, j, checkAgents))
                {
                    return true;
                }
            }
        }
        return false;
    }
    public bool IsTileBlocked(GridMoveAgent agent, Vector3 pos, bool checkAgents)
    {
        if (!GetTileXZUnclamped(pos, out var x, out var z))
        {
            return true;
        }
        return IsTileBlocked(agent, x, z, checkAgents);
    }
    public bool IsCrossWalkable(GridMoveAgent agent, Vector3 startPos, Vector3 endPos, bool checkAgents)
    {
        if (!GetTileXZUnclamped(startPos, out var startX, out var startZ)
            || !GetTileXZUnclamped(endPos, out var endX, out var endZ))
        {
            return false;
        }
        if (IsTileBlocked(agent, startX, startZ, checkAgents))
        {
            return false;
        }
        var snode = pathFinder.GetNode(startX, startZ);
        var enode = pathFinder.GetNode(endX, endZ);
        if (snode == null || enode == null)
        {
            return true;
        }
        return pathFinder.IsCrossWalkable(agent.UnitSize, snode, enode, (int x, int z) => { return IsTileCenterBlocked(agent, x, z, checkAgents); });
    }
    public void ForeachAgents(Vector3 pos, float radius, Func<GridMoveAgent, bool> func)
    {
        var arr = new List<GridMoveAgent>();
        foreach (var agent in agents)
        {
            float r = radius + agent.Radius;
            if (GridMathUtils.SqrDistance2D(pos, agent.Pos) <= r * r)
            {
                if (!func(agent))
                {
                    return;
                }
            }
        }
    }
    public void OnTileChange(GridMoveAgent agent, int oldX, int oldZ, int newX, int newZ)
    {
        if (oldX == newX && oldZ == newZ)
        {
            return;
        }
        int offset = (agent.UnitSize >> 1);

        int xmin = Mathf.Max(oldX - offset, 0);
        int xmax = Mathf.Min(oldX + offset, xsize - 1);
        int zmin = Mathf.Max(oldZ - offset, 0);
        int zmax = Mathf.Min(oldZ + offset, zsize - 1);
        for (int z = zmin; z <= zmax; z++)
        {
            for (int x = xmin; x <= xmax; x++)
            {
                tiles[x + z * xsize].agents.Remove(agent);
            }
        }
        xmin = Mathf.Max(newX - offset, 0);
        xmax = Mathf.Min(newX + offset, xsize - 1);
        zmin = Mathf.Max(newZ - offset, 0);
        zmax = Mathf.Min(newZ + offset, zsize - 1);
        for (int z = zmin; z <= zmax; z++)
        {
            for (int x = xmin; x <= xmax; x++)
            {
                tiles[x + z * xsize].agents.Add(agent);
            }
        }
    }
    public bool FindPath(GridMoveAgent agent, Vector3 startPos, Vector3 goalPos, float goalRadius, out GridPath path)
    {
        GetTileXZ(startPos, out var startX, out var startZ);
        GetTileXZ(goalPos, out var goalX, out var goalZ);

        path = new GridPath();

        Func<int, int, bool> func = (int x, int z) => { return IsTileCenterBlocked(agent, x, z, true); };
        var startNode = pathFinder.FindNearestNode(agent.UnitSize, startX, startZ, agent.UnitSize * 3, func);
        var goalNode = pathFinder.GetNode(goalX, goalZ);
        if (startNode == null || goalNode == null)
        {
            return false;
        }
        int radius = (int)(goalRadius / tileSize);
        pathFinder.FindPath(agent.UnitSize, startNode, goalNode, radius, 1024, 1024, func, out var nodePath);
        foreach (var n in nodePath)
        {
            path.positions.Add(GetTilePos(n.X, n.Z));
        }
        return true;
    }
    public Vector3 NextWayPoint(GridMoveAgent agent, ref GridPath path, Vector3 pos, float distance)
    {
        while (path.positions.Count > 1)
        {
            if ((pos - path.positions[0]).sqrMagnitude > distance)
            {
                break;
            }
            path.positions.RemoveAt(0);
        }
        return path.positions[0];
    }
}