using UnityEngine;

public class GridNavMesh
{
    private Vector3 bmin;
    private int xsize;
    private int zsize;
    private float squareSize;
    private int[] squareTypeMap; // xsize * zsize origin data
    private float[] cornerHeightMap; // (xsize + 1) * (zsize + 1) origin data
    private float[] centerHeightMap; // xsize * zsize
    private Vector3[] faceNormals; // xsize * zsize * 2
    private Vector3[] centerNormals; // xsize * zsize
    private Vector3[] centerNormals2D; // xsize * zsize
    private float[] slopeMap; // (xsize / 2) * (zsize / 2)

    public int XSize { get => xsize; }
    public int ZSize { get => zsize; }
    public float SquareSize { get => squareSize; }

    public bool Init(Vector3 bmin, int xsize, int zsize, float squareSize)
    {
        if (xsize < 1 || zsize < 1 || squareSize <= 0.0f)
        {
            return false;
        }
        this.bmin = bmin;
        this.xsize = xsize;
        this.zsize = zsize;
        this.squareSize = squareSize;
        this.squareTypeMap = new int[xsize * zsize];
        this.cornerHeightMap = new float[(xsize + 1) * (zsize + 1)];
        this.centerHeightMap = new float[xsize * zsize];
        this.faceNormals = new Vector3[xsize * zsize * 2];
        this.centerNormals = new Vector3[xsize * zsize];
        this.centerNormals2D = new Vector3[xsize * zsize];
        this.slopeMap = new float[(xsize / 2) * (zsize / 2)];
        return true;
    }
    public void Clear()
    {
    }
    public void SetSquareType(int x, int z, int type)
    {
        Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
        squareTypeMap[x + z * xsize] = type;
    }
    public void SetCornerHeight(int x, int z, float height)
    {
        Debug.Assert(x >= 0 && x <= xsize && z >= 0 && z <= zsize);
        cornerHeightMap[x + z * (xsize + 1)] = height;
    }
    public void UpdateHeightMap()
    {
    }




    public void GetSquareXZ(int index, out int x, out int z)
    {
        x = index & 0xFFFF;
        z = index >> 16;
        Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
    }
    public int GetSquareIndex(int x, int z)
    {
        Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
        return x + (z << 16);
    }
    public void GetSquareXZ(Vector3 pos, out int x, out int z)
    {
        x = (int)((pos.x - bmin.x) / squareSize);
        z = (int)((pos.z - bmin.z) / squareSize);
        x = Mathf.Clamp(x, 0, xsize - 1);
        z = Mathf.Clamp(z, 0, zsize - 1);
    }
    public int GetSquareIndex(Vector3 pos)
    {
        int x = (int)((pos.x - bmin.x) / squareSize);
        int z = (int)((pos.z - bmin.z) / squareSize);
        x = Mathf.Clamp(x, 0, xsize - 1);
        z = Mathf.Clamp(z, 0, zsize - 1);
        return GetSquareIndex(x, z);
    }
    public int GetSquareCenterIndex(int startIndex, int endIndex)
    {
        GetSquareXZ(startIndex, out var sx, out var sz);
        GetSquareXZ(endIndex, out var ex, out var ez);
        var mx = (sx + ex) >> 1;
        var mz = (sz + ez) >> 1;
        return GetSquareIndex(mx, mz);
    }
    public int GetSuqareNeighbourIndex(int index, GridNavDirection dir)
    {
        GetSquareXZ(index, out var x, out var z);
        x += dirX[(int)dir];
        z += dirZ[(int)dir];
        if (x < 0 || x >= xsize || z < 0 || z >= zsize)
        {
            return -1;
        }
        return GetSquareIndex(x, z);
    }
    public void ClampInBounds(Vector3 pos, out int nearestIndex, out Vector3 nearestPos)
    {
        int x = (int)((pos.x - bmin.x) / squareSize);
        int z = (int)((pos.z - bmin.z) / squareSize);
        if (x < 0 || x >= xsize || z < 0 || z >= zsize)
        {
            x = Mathf.Clamp(x, 0, xsize - 1);
            z = Mathf.Clamp(z, 0, zsize - 1);
            nearestIndex = GetSquareIndex(x, z);
            nearestPos = GetSquarePos(nearestIndex);
        }
        else
        {
            nearestIndex = GetSquareIndex(x, z);
            nearestPos = pos;
        }
    }
    public void SetSquare(int index, float cost, bool isBlocked)
    {
        GetSquareXZ(index, out var x, out var z);
        SetSquare(x, z, cost, isBlocked);
    }
    public void SetSquare(int x, int z, float cost, bool isBlocked)
    {
        Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
        var square = squares[x + z * xsize];
        square.cost = cost;
        square.isBlocked = isBlocked;
    }
    public Vector3 GetSquarePos(int index)
    {
        GetSquareXZ(index, out var x, out var z);
        return new Vector3(bmin.x + (x + 0.5f) * squareSize, 0, bmin.z + (z + 0.5f) * squareSize);
    }
    public bool IsSquareBlocked(int index)
    {
        GetSquareXZ(index, out var x, out var z);
        return squares[x + z * xsize].isBlocked;
    }
    public float GetSquareCost(int index, GridNavDirection dir)
    {
        GetSquareXZ(index, out var x, out var z);
        return squares[x + z * xsize].cost + dirCost[(int)dir] * squareSize;
    }
    public float DistanceApproximately(int startIndex, int endIndex)
    {
        GetSquareXZ(startIndex, out var sx, out var sz);
        GetSquareXZ(endIndex, out var ex, out var ez);
        int dx = Mathf.Abs(ex - sx);
        int dz = Mathf.Abs(ez - sz);
        return ((dx + dz) + Mathf.Min(dx, dz) * (1.4142f - 2.0f)) * squareSize;
    }
    public float SqrDistance(int startIndex, int endIndex)
    {
        GetSquareXZ(startIndex, out var sx, out var sz);
        GetSquareXZ(endIndex, out var ex, out var ez);
        int dx = Mathf.Abs(ex - sx);
        int dz = Mathf.Abs(ez - sz);
        return (dx * dx + dz * dz) * squareSize * squareSize;
    }
    private void UpdateCenterHeightMap(int xmin, int xmax, int zmin, int zmax)
    {
        for (int z = zmin; z <= zmax; z++)
        {
            for (int x = xmin; x <= xmax; x++)
            {
                float height = cornerHeightMap[x + z * (xsize + 1)]
                    + cornerHeightMap[x + 1 + z * (xsize + 1)]
                    + cornerHeightMap[x + (z + 1) * (xsize + 1)]
                    + cornerHeightMap[(x + 1) + (z + 1) * (xsize + 1)];

                centerHeightMap[x + z * xsize] = height * 0.25f;
            }
        }
    }
    private void UpdateFaceNormals(int xmin, int xmax, int zmin, int zmax)
    {
        xmin = Mathf.Max(0, xmin - 1);
        xmax = Mathf.Min(xsize - 1, xmax + 1);
        zmin = Mathf.Max(0, zmin - 1);
        zmax = Mathf.Min(zsize - 1, zmax + 1);
        for (int z = zmin; z <= zmax; z++)
        {
            for (int x = xmin; x <= xmax; x++)
            {
                float hTL = cornerHeightMap[x + z * (xsize + 1)];
                float hTR = cornerHeightMap[x + 1 + z * (xsize + 1)];
                float hBL = cornerHeightMap[x + (z + 1) * (xsize + 1)];
                float hBR = cornerHeightMap[(x + 1) + (z + 1) * (xsize + 1)];
            }
        }
    }
}