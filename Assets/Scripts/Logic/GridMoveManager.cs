using System.Collections.Generic;
using UnityEngine;

public class GridMoveManager
{
    class Grid
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

    int gameSpeed = 30;
    int frameNum = 0;
    Vector3 pos = Vector3.zero;
    int gridX = 11;
    int gridZ = 11;
    float gridSize = 0.2f;

    Grid[] grids;
    GridMoveAgent[] agents;
    GridPathFinder pathFinder;

    public GridMoveManager(int gameSpeed, Vector3 pos, int gridX, int gridZ, float gridSize, int maxAgent)
    {
        this.gameSpeed = gameSpeed;
        this.frameNum = 0;
        this.pos = pos;
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