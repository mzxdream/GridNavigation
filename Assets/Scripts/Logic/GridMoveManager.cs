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

    private readonly int gameSpeed = 30;
    private readonly Vector3 pos = Vector3.zero;
    private readonly int gridX = 11;
    private readonly int gridZ = 11;
    private readonly float gridSize = 0.2f;

    private int frameNum = 0;
    private Grid[] grids;
    private GridMoveAgent[] agents;
    private GridPathFinder pathFinder;

    public GridMoveManager(int gameSpeed, Vector3 pos, int gridX, int gridZ, float gridSize, int maxAgent)
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
        this.agents = new GridMoveAgent[maxAgent];
        this.pathFinder = new GridPathFinder(gridX, gridZ);
    }

    public void Update()
    {

    }

    public void LateUpdate()
    {

    }
}