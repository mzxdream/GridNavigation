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

    private int gameSpeed = 30;
    private Vector3 bmin = Vector3.zero;
    private Vector3 bmax = Vector3.zero;
    private int gridX = 11;
    private int gridZ = 11;
    private float gridSize = 0.2f;

    private int frameNum = 0;
    private Grid[] grids;
    private List<GridMoveAgent> agents; //TODO optimization
    private GridPathFinder pathFinder;

    public GridMoveManager()
    {
    }
    public bool Init(int gameSpeed, Vector3 pos, int gridX, int gridZ, float gridSize, int maxAgents)
    {
        this.gameSpeed = gameSpeed;
        var offset = new Vector3(gridX * gridSize / 2, 0, gridZ * gridSize / 2);
        this.bmin = pos - offset;
        this.bmax = pos + offset;
        this.gridX = gridX;
        this.gridZ = gridZ;
        this.gridSize = gridSize;

        this.frameNum = 0;
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
    public void ClampInBounds(ref Vector3 pos)
    {
        pos.x = Mathf.Clamp(pos.x, bmin.x, bmax.x);
        pos.z = Mathf.Clamp(pos.z, bmin.z, bmax.z);
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
}