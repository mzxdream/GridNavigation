using UnityEngine;

public class PathManager : Singleton<PathManager>
{
    public int RequestPath(Unit caller, Vector3 startPos, Vector3 goalPos, float goalRadius)
    {
        return 0;
    }
    public void DeletaPath(int pathID)
    {
    }
    public void UpdatePath(Unit owner, int pathID)
    {
    }
    public Vector3 NextWayPoint(Unit owner, int pathID, Vector3 callerPos, float radius)
    {
        return Vector3.zero;
    }
    public void TerrainChange(int xmin, int zmin, int xmax, int zmax)
    {
        
    }
}