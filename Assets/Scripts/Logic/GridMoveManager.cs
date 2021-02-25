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
    public Vector3 startPos;
    public Vector3 goalPos;
    public float goalRadius;
    public List<Vector3> positions;
}

 private GridPathNode FindNearestNode(GridMoveAgent agent, Vector3 pos, float searchRadius)
    {
        moveManager.GetTileXZ(pos, out int x, out int z);
        if (!IsNodeBlocked(agent, x, z))
        {
            return nodes[x + z * xsize];
        }
        var searchSize = (int)(searchRadius / tileSize);
        for (int i = 0; i < searchSize; i++)
        {
            if (!IsNodeBlocked(agent, x + i, z))
            {
                return nodes[x + i + z * xsize];
            }
            if (!IsNodeBlocked(agent, x - i, z))
            {
                return nodes[x - i + z * xsize];
            }
            if (!IsNodeBlocked(agent, x, z + i))
            {
                return nodes[x + (z + i) * xsize];
            }
            if (!IsNodeBlocked(agent, x, z - i))
            {
                return nodes[x + (z - i) * xsize];
            }
            if (!IsNodeBlocked(agent, x + i, z + i))
            {
                return nodes[x + i + (z + i) * xsize];
            }
            if (!IsNodeBlocked(agent, x + i, z - i))
            {
                return nodes[x + i + (z - i) * xsize];
            }
            if (!IsNodeBlocked(agent, x - i, z + i))
            {
                return nodes[x - i + (z + i) * xsize];
            }
            if (!IsNodeBlocked(agent, x - i, z - i))
            {
                return nodes[x - i + (z - i) * xsize];
            }
        }
        return null;
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

        tiles = new GridTileInfo[xsize * zsize];
        for (int z = 0; z < zsize; z++)
        {
            for (int x = 0; x < xsize; x++)
            {
                var index = x + z * xsize;
                tiles[index] = new GridTileInfo { index = index, isBlocked = false, agents = new List<GridMoveAgent>() };
            }
        }
        agents = new List<GridMoveAgent>();
        pathFinder = new GridPathFinder();
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
    public bool TestMoveRange(GridMoveAgent agent, Vector3 rmin, Vector3 rmax, bool checkAgents)
    {
        if (!GetTileXZUnclamped(rmin, out var xmin, out var zmin)
            || !GetTileXZUnclamped(rmax, out var xmax, out var zmax))
        {
            return false;
        }
        for (int z = zmin; z <= zmax; z++)
        {
            for (int x = xmin; x <= xmax; x++)
            {
                var grid = grids[x + z * xsize];
                if (grid.isBlocked)
                {
                    return false;
                }
                if (checkAgents)
                {
                    foreach (var a in grid.agents)
                    {
                        if (a.IsBlockedOther(agent))
                        {
                            return false;
                        }
                    }
                }
            }
        }
        return true;
    }
    public List<GridMoveAgent> GetUnitsExact(Vector3 pos, float radius)
    {
        var arr = new List<GridMoveAgent>();
        foreach (var agent in agents)
        {
            float r = radius + agent.GetRadius();
            if (GridMathUtils.SqrDistance2D(pos, agent.Pos) <= r * r)
            {
                arr.Add(agent);
            }
        }
        return arr;
    }
    public void OnPositionChange(GridMoveAgent agent, Vector3 pos)
    {
        if (agentGridIndexes.TryGetValue(agent.ID, out int oldIndex))
        {
            int oldX = oldIndex % xsize, oldZ = oldIndex / xsize;
            int xmin = Mathf.Max(oldX - agent.UnitSize / 2, 0);
            int xmax = Mathf.Min(oldX + agent.UnitSize / 2, xsize - 1);
            int zmin = Mathf.Max(oldZ - agent.UnitSize / 2, 0);
            int zmax = Mathf.Min(oldZ + agent.UnitSize / 2, zsize - 1);
            for (int z = zmin; z <= zmax; z++)
            {
                for (int x = xmin; x <= xmax; x++)
                {
                    var grid = grids[x + z * xsize];
                    grid.agents.Remove(agent);
                }
            }
            agentGridIndexes.Remove(agent.ID);
        }
        {
            var newIndex = GetGridIndex(pos);
            agentGridIndexes.Add(agent.ID, newIndex);
            int newX = newIndex % xsize, newZ = newIndex / xsize;
            int xmin = Mathf.Max(newX - agent.UnitSize / 2, 0);
            int xmax = Mathf.Min(newX + agent.UnitSize / 2, xsize - 1);
            int zmin = Mathf.Max(newZ - agent.UnitSize / 2, 0);
            int zmax = Mathf.Min(newZ + agent.UnitSize / 2, zsize - 1);
            for (int z = zmin; z <= zmax; z++)
            {
                for (int x = xmin; x <= xmax; x++)
                {
                    var grid = grids[x + z * xsize];
                    grid.agents.Add(agent);
                }
            }
        }
    }
}