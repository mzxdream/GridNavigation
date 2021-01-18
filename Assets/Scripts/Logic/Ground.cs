using System.Collections.Generic;
using UnityEngine;

public class Ground : Singleton<Ground>
{
    int gridX;
    int gridZ;
    float gridSize;
    Dictionary<int, List<Unit>> blockingObjs = new Dictionary<int, List<Unit>>();
    public bool Init(int gridX, int gridZ, float gridSize)
    {
        this.gridX = gridX;
        this.gridZ = gridZ;
        this.gridSize = gridSize;
        return true;
    }
    public int GetSquare(Vector3 pos)
    {
        int tx = (int)((pos.x + gridX * gridSize / 2) / gridSize);
        int tz = (int)((pos.z + gridZ * gridSize / 2) / gridSize);
        tx = Mathf.Clamp(tx, 0, gridX - 1);
        tz = Mathf.Clamp(tz, 0, gridZ - 1);
        return tx + tz * gridX;
    }
    public Vector3 GetSquarePos(int tx, int tz)
    {
        Debug.Assert(tx >= 0 && tx < gridX && tz >= 0 && tz < gridZ);
        float x = (tx + 0.5f - gridX * 0.5f) * gridSize;
        float z = (tz + 0.5f - gridZ * 0.5f) * gridSize;
        return new Vector3(x, 0, z);
    }
    public void BlockingObjChange(Unit obj, int oldMapSquare, int newMapSquare)
    {
        int xmin = (oldMapSquare % gridX) - obj.XSize / 2;
        int xmax = xmin + obj.XSize - 1;
        int zmin = (oldMapSquare / gridX) - obj.ZSize / 2;
        int zmax = zmin + obj.ZSize - 1;
        for (int i = xmin; i <= xmax; i++)
        {
            for (int j = zmin; j <= zmax; j++)
            {
                int key = i + j * gridX;
                if (blockingObjs.TryGetValue(key, out var objs))
                {
                    objs.Remove(obj);
                }
            }
        }
        PathManager.Instance.TerrainChange(xmin, xmax, zmin, zmax);
        
        xmin = (newMapSquare % gridX) - obj.XSize / 2;
        xmax = xmin + obj.XSize - 1;
        zmin = (newMapSquare / gridX) - obj.ZSize / 2;
        zmax = zmin + obj.ZSize - 1;
        for (int i = xmin; i <= xmax; i++)
        {
            for (int j = zmin; j <= zmax; j++)
            {
                int key = i + j * gridX;
                if (blockingObjs.TryGetValue(key, out var objs))
                {
                    objs.Add(obj);
                }
                else
                {
                    objs = new List<Unit>();
                    objs.Add(obj);
                    blockingObjs.Add(key, objs);
                }
            }
        }
    }
}