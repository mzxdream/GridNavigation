using System.Collections.Generic;
using UnityEngine;

public class Ground : Singleton<Ground>
{
    int gridX;
    int gridZ;
    float gridSize;
    public float GridSize { get => gridSize; }
    Vector3 bmin;
    Vector3 bmax;
    Dictionary<int, List<Unit>> blockingObjs = new Dictionary<int, List<Unit>>();
    public bool Init(Vector3 pos, int gridX, int gridZ, float gridSize)
    {
        this.gridX = gridX;
        this.gridZ = gridZ;
        this.gridSize = gridSize;

        var tmp = new Vector3(gridX * gridSize / 2.0f, 0f, gridZ * gridSize / 2.0f);
        bmin = pos - tmp;
        bmax = pos + tmp;
        return true;
    }
    public int GetSquare(Vector3 pos)
    {
        Debug.Assert(pos.x >= bmin.x && pos.x <= bmax.x && pos.z >= bmax.x && pos.z <= bmax.z);
        int tx = (int)((pos.x - bmin.x + gridSize / 2) / gridSize);
        int tz = (int)((pos.z - bmin.z + gridSize / 2) / gridSize);
        tx = Mathf.Clamp(tx, 0, gridX - 1);
        tz = Mathf.Clamp(tz, 0, gridZ - 1);
        return tx + tz * gridX;
    }
    public Vector3 GetCenter(int tx, int tz)
    {
        Debug.Assert(tx >= 0 && tx < gridX && tz >= 0 && tz < gridZ);
        float x = bmin.x + (tx + 0.5f) * gridSize;
        float z = bmin.z + (tz + 0.5f) * gridSize;
        return new Vector3(x, 0, z);
    }
    public bool ClampInBounds(Vector3 pos, out Vector3 newPos)
    {
        if (pos.x >= bmin.x && pos.x <= bmax.x && pos.z >= bmin.z && pos.z <= bmax.z)
        {
            newPos = pos;
            return false;
        }
        newPos = new Vector3(Mathf.Clamp(pos.x, bmin.x, bmax.x), pos.y, Mathf.Clamp(pos.z, bmin.z, bmax.z));
        return true;
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
        PathManager.Instance.TerrainChange(xmin, xmax, zmin, zmax);
    }
    public bool TestMoveSquareRange(Unit collider, Vector3 rangeMins, Vector3 rangeMax, Vector3 testMoveDir, bool testTerrain, bool testObjs, bool centerOnly)
    {
        return true;
    }
    public bool SquareIsBlocked(Unit collider, Vector3 pos)
    {
        return false;
    }
    public List<Unit> GetSolids(Vector3 pos, float radius)
    {
        return null;
    }
    public bool IsNonBlocking(Unit avoider, Unit avoidee)
    {
        return true;
    }
}