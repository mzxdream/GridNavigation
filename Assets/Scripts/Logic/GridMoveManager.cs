using System.Collections.Generic;
using UnityEngine;

public class GridMoveManager
{
    class Grid
    {
        int x;
        int z;
        bool isBlocked;
        List<GridMoveAgent> agents;
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