using UnityEngine;

public class GridMoveManager
{
    int gameSpeed = 30;
    int frameNum = 0;
    Vector3 pos = Vector3.zero;
    int gridX = 11;
    int gridZ = 11;
    float gridSize = 0.2f;
    GridPathFinder pathFinder;

    public GridMoveManager(int gameSpeed, Vector3 pos, int gridX, int gridZ, float gridSize)
    {
        this.gameSpeed = gameSpeed;
        this.frameNum = 0;
        this.pos = pos;
        this.gridX = gridX;
        this.gridZ = gridZ;
        this.gridSize = gridSize;
        this.pathFinder = new GridPathFinder(gridX, gridZ);
    }


}