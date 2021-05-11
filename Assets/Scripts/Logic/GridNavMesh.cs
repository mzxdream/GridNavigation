using UnityEngine;

class GridNavSquare
{
    public float cost;
    public bool isBlocked;
}

public class GridNavMesh
{
    private static readonly int[] dirX = { 0, -1, 1, 0, 0, -1, 1, -1, 1 };
    private static readonly int[] dirZ = { 0, 0, 0, 1, -1, 1, 1, -1, -1 };
    private static readonly float[] dirCost = { 0, 1.0f, 1.0f, 1.0f, 1.0f, 1.4142f, 1.4142f, 1.4142f, 1.4142f };
    private Vector3 bmin;
    private int xsize;
    private int zsize;
    private float squareSize;
    private GridNavSquare[] squares;

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
        this.squares = new GridNavSquare[xsize * zsize];
        for (int z = 0; z < zsize; z++)
        {
            for (int x = 0; x < xsize; x++)
            {
                this.squares[x + z * xsize] = new GridNavSquare
                {
                    cost = 1.0f,
                    isBlocked = false,
                };
            }
        }
        return true;
    }
    public void Clear()
    {
    }
    public void GetSquareXZ(int index, out int x, out int z)
    {
        z = index / 100;
        x = index - z * 100;
        Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
    }
    public int GetSquareIndex(int x, int z)
    {
        Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
        return x + z * 100;
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
}