using System.Collections.Generic;
using UnityEngine;

public class GridMoveManager
{
    private class Grid
    {
        public readonly int x;
        public readonly int z;
        public bool isBlocked;
        public List<GridMoveAgent> agents = new List<GridMoveAgent>();

        public Grid(int x, int z)
        {
            this.x = x;
            this.z = z;
        }
    }

    private Vector3 bmin = Vector3.zero;
    private Vector3 bmax = Vector3.zero;
    private int gridX = 11;
    private int gridZ = 11;
    private float gridSize = 0.2f;

    private Grid[] grids;
    private List<GridMoveAgent> agents; //TODO optimization
    private GridPathFinder pathFinder;

    public float GridSize { get => gridSize; }
    public float GameSpeed { get => 30; }

    public GridMoveManager()
    {
    }
    public bool Init(Vector3 pos, int gridX, int gridZ, float gridSize, int maxAgents)
    {
        var offset = new Vector3(gridX * gridSize / 2, 0, gridZ * gridSize / 2);
        this.bmin = pos - offset;
        this.bmax = pos + offset;
        this.gridX = gridX;
        this.gridZ = gridZ;
        this.gridSize = gridSize;

        this.grids = new Grid[gridX * gridZ];
        for (int z = 0; z < gridZ; z++)
        {
            for (int x = 0; x < gridX; x++)
            {
                this.grids[x + z * gridX] = new Grid(x, z);
            }
        }
        this.agents = new List<GridMoveAgent>();
        this.pathFinder = new GridPathFinder(gridX, gridZ);
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
        x = (int)((pos.x - bmin.x) / gridSize);
        z = (int)((pos.z - bmin.z) / gridSize);
        x = Mathf.Clamp(x, 0, gridX - 1);
        z = Mathf.Clamp(z, 0, gridZ - 1);
    }
    public int GetGridIndex(Vector3 pos)
    {
        int x = (int)((pos.x - bmin.x) / gridSize);
        int z = (int)((pos.z - bmin.z) / gridSize);
        return Mathf.Clamp(x, 0, gridX - 1) + Mathf.Clamp(z, 0, gridZ - 1) * gridX;
    }
    public Vector3 GetGridPos(int index)
    {
        Debug.Assert(index >= 0 && index < gridX * gridZ);
        int z = index / gridX;
        int x = index - z * gridX;
        return new Vector3(bmin.x + (x + 0.5f) * gridSize, 0, bmin.z + (z + 0.5f) * gridSize);
    }
    public Vector3 GetGridPos(int x, int z)
    {
        return GetGridPos(x + z * gridX);
    }
    public GridPath FindPath(GridMoveAgent agent, Vector3 goalPos, float goalRadius)
    {
        GetGirdXZ(agent.Pos, out int startX, out int startZ);
        GetGirdXZ(goalPos, out int goalX, out int goalZ);
        var path = pathFinder.Search(agent.UnitSize, startX, startZ, goalX, goalZ, (int)(goalRadius / gridSize), 8192, 8192, (int x, int z) =>
        {
            Grid grid = grids[x + z * gridX];
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
    public bool IsGridBlocked(GridMoveAgent agent, Vector3 pos)
    {
        GetGirdXZ(pos, out int x, out int z);
        if (x < 0 || x >= gridX || z < 0 || z >= gridZ)
        {
            return true;
        }
        var grid = grids[x + z * gridX];
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
    }
    public bool TestMoveRange(GridMoveAgent agent, Vector3 rmin, Vector3 rmax, bool checkAgents)
    {
        GetGirdXZ(rmin, out int xmin, out int zmin);
        GetGirdXZ(rmax, out int xmax, out int zmax);
        if (xmin < 0 || xmax >= gridX || zmin < 0 || zmax >= gridZ)
        {
            return false;
        }
        for (int z = zmin; z <= zmax; z++)
        {
            for (int x = xmin; x <= xmax; x++)
            {
                var grid = grids[x + z * gridX];
                if (grid.isBlocked)
                {
                    return false;
                }
                foreach (var a in grid.agents)
                {
                    if (a.IsBlockedOther(agent))
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
}