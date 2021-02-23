using System.Collections.Generic;
using UnityEngine;

public class GridMoveManager
{
    class TileInfo
    {
        public int index;
        public bool isBlocked;
        public List<GridMoveAgent> agents = new List<GridMoveAgent>();
    }

    private Vector3 bmin;
    private Vector3 bmax;
    private int xsize;
    private int zsize;
    private float tileSize;
    private int gameSpeed;

    private TileInfo[] tiles;
    private List<GridMoveAgent> agents;
    private GridPathFinder pathFinder;

    public int XSize { get => xsize; }
    public int ZSize { get => zsize; }
    public float TileSize { get => tileSize; }
    public float PixelSize { get => tileSize / 8.0f; }
    public int GameSpeed { get => gameSpeed; }

    public bool Init(Vector3 bmin, Vector3 bmax, float tileSize, int gameSpeed, int maxAgents)
    {
        this.bmin = bmin;
        this.bmax = bmax;
        this.tileSize = tileSize;
        xsize = (int)((bmax.x - bmin.x) / tileSize);
        zsize = (int)((bmax.z - bmin.z) / tileSize);

        tiles = new TileInfo[xsize * zsize];
        for (int z = 0; z < zsize; z++)
        {
            for (int x = 0; x < xsize; x++)
            {
                var index = x + z * xsize;
                tiles[index] = new TileInfo
                {
                    index = index,
                    isBlocked = false,
                };
            }
        }
        this.agents = new List<GridMoveAgent>();
        this.pathFinder = new GridPathFinder(this);
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
    public void GetGirdXZ(Vector3 pos, out int x, out int z)
    {
        x = (int)((pos.x - bmin.x) / tileSize);
        z = (int)((pos.z - bmin.z) / tileSize);
        x = Mathf.Clamp(x, 0, xsize - 1);
        z = Mathf.Clamp(z, 0, zsize - 1);
    }
    public int GetGridIndex(Vector3 pos)
    {
        int x = (int)((pos.x - bmin.x) / tileSize);
        int z = (int)((pos.z - bmin.z) / tileSize);
        return Mathf.Clamp(x, 0, xsize - 1) + Mathf.Clamp(z, 0, zsize - 1) * xsize;
    }
    public Vector3 GetGridPos(int index)
    {
        Debug.Assert(index >= 0 && index < xsize * zsize);
        int z = index / xsize;
        int x = index - z * xsize;
        return new Vector3(bmin.x + (x + 0.5f) * tileSize, 0, bmin.z + (z + 0.5f) * tileSize);
    }
    public Vector3 GetGridPos(int x, int z)
    {
        return GetGridPos(x + z * xsize);
    }
    public GridPath FindPath(GridMoveAgent agent, Vector3 goalPos, float goalRadius)
    {
        GetGirdXZ(agent.Pos, out int startX, out int startZ);
        GetGirdXZ(goalPos, out int goalX, out int goalZ);
        var path = pathFinder.Search(agent.UnitSize, startX, startZ, goalX, goalZ, (int)(goalRadius / tileSize), 8192, 8192, (int x, int z) =>
        {
            Grid grid = grids[x + z * xsize];
            if (grid.isBlocked)
            {
                return true;
            }
            foreach (var a in grid.agents)
            {
                if (a.IsBlockedOther(agent))
                {
                    return true;
                }
            }
            return false;
        });
        if (path != null)
        {
            path.goalPos = goalPos;
        }
        return path;
    }
    public Vector3 NextWayPoint(GridMoveAgent agent, GridPath path, Vector3 pos, float distance)
    {
        var h = path.Head.Next;
        while (h != path.Head)
        {
            var waypoint = GetGridPos(h.X, h.Z);
            h.Erase();
            if (GridMathUtils.SqrDistance2D(pos, waypoint) >= distance * distance)
            {
                return waypoint;
            }
            h = path.Head.Next;
        }
        return path.goalPos;
        //return GetGridPos(path.goalNode.X, path.goalNode.Z);
    }
    public bool IsGridBlocked(GridMoveAgent agent, Vector3 pos, bool checkAgents)
    {
        GetGirdXZ(pos, out int x, out int z);
        if (x < 0 || x >= xsize || z < 0 || z >= zsize)
        {
            return true;
        }
        var grid = grids[x + z * xsize];
        if (grid.isBlocked)
        {
            return true;
        }
        if (checkAgents)
        {
            foreach (var a in grid.agents)
            {
                if (a.IsBlockedOther(agent))
                {
                    return true;
                }
            }
        }
        return false;
    }
    public bool IsGridBlocked(int x, int z)
    {
        if (x < 0 || x >= xsize || z < 0 || z >= zsize)
        {
            return true;
        }
        var grid = grids[x + z * xsize];
        return grid.isBlocked;
    }
    public bool TestMoveRange(GridMoveAgent agent, Vector3 rmin, Vector3 rmax, bool checkAgents)
    {
        GetGirdXZ(rmin, out int xmin, out int zmin);
        GetGirdXZ(rmax, out int xmax, out int zmax);
        if (xmin < 0 || xmax >= xsize || zmin < 0 || zmax >= zsize)
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