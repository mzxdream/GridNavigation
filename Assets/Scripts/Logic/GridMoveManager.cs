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
    private Vector3 pos = Vector3.zero;
    private int gridX = 11;
    private int gridZ = 11;
    private float gridSize = 0.2f;

    private int frameNum = 0;
    private Grid[] grids;
    private GridMoveAgent[] agents;
    private GridPathFinder pathFinder;

    public GridMoveManager()
    {
    }
    public bool Init(int gameSpeed, Vector3 pos, int gridX, int gridZ, float gridSize, int maxAgents)
    {
        this.gameSpeed = gameSpeed;
        this.pos = pos;
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
        this.agents = new GridMoveAgent[maxAgents];
        this.pathFinder = new GridPathFinder(gridX, gridZ);
        return true;
    }
    public void Clear()
    {
    }
    public void Update()
    {

    }

    public void LateUpdate()
    {

    }
}