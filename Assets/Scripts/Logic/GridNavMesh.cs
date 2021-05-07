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
        Debug.Assert(index >= 0 && index < xsize * zsize);
        z = index / xsize;
        x = index - z * xsize;
    }
    public void GetSquareXZ(Vector3 pos, out int x, out int z)
    {
        x = (int)((pos.x - bmin.x) / squareSize);
        z = (int)((pos.z - bmin.z) / squareSize);
        x = Mathf.Clamp(x, 0, xsize - 1);
        z = Mathf.Clamp(z, 0, zsize - 1);
    }
    public int GetSquareIndex(int x, int z)
    {
        Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
        return x + z * xsize;
    }
    public int GetSquareCenterIndex(int startIndex, int endIndex)
    {
        GetSquareXZ(startIndex, out var sx, out var sz);
        GetSquareXZ(endIndex, out var ex, out var ez);
        return (sx + ex) / 2 + (ex + ez) / 2 * zsize;
    }
    public int GetSuqareNeighbourIndex(int index, GridNavDirection dir)
    {
        Debug.Assert(index >= 0 && index < xsize * zsize);
        int z = index / xsize;
        int x = index - z * xsize;
        x += dirX[(int)dir];
        z += dirZ[(int)dir];
        if (x < 0 || x >= xsize || z < 0 || z >= zsize)
        {
            return -1;
        }
        return x + z * xsize;
    }
    public void ClampInBounds(Vector3 pos, out int nearestIndex, out Vector3 nearestPos)
    {
        int x = (int)((pos.x - bmin.x) / squareSize);
        int z = (int)((pos.z - bmin.z) / squareSize);
        if (x < 0 || x >= xsize || z < 0 || z >= zsize)
        {
            x = Mathf.Clamp(x, 0, xsize - 1);
            z = Mathf.Clamp(z, 0, zsize - 1);
            nearestIndex = x + z * xsize;
            nearestPos = GetSquarePos(nearestIndex);
        }
        else
        {
            nearestIndex = x + z * xsize;
            nearestPos = pos;
        }
    }
    public void SetSquare(int index, float cost, bool isBlocked)
    {
        Debug.Assert(index >= 0 && index < xsize * zsize);
        var square = squares[index];
        square.cost = cost;
        square.isBlocked = isBlocked;
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
        Debug.Assert(index >= 0 && index < xsize * zsize);
        int z = index / xsize;
        int x = index - z * xsize;
        return new Vector3(bmin.x + (x + 0.5f) * squareSize, 0, bmin.z + (z + 0.5f) * squareSize);
    }
    public bool IsSquareBlocked(int index)
    {
        Debug.Assert(index >= 0 && index < xsize * zsize);
        return squares[index].isBlocked;
    }
    public float GetSquareCost(int index, GridNavDirection dir)
    {
        Debug.Assert(index >= 0 && index < xsize * zsize);
        return squares[index].cost + dirCost[(int)dir] * squareSize;
    }
    public float DistanceApproximately(int startIndex, int endIndex)
    {
        Debug.Assert(startIndex >= 0 && startIndex < xsize * zsize && endIndex >= 0 && endIndex < xsize * zsize);
        int sz = startIndex / xsize;
        int sx = startIndex - sz * xsize;
        int ez = endIndex / xsize;
        int ex = endIndex - ez * xsize;
        int dx = Mathf.Abs(ex - sx);
        int dz = Mathf.Abs(ez - sz);
        return ((dx + dz) + Mathf.Min(dx, dz) * (1.4142f - 2.0f)) * squareSize;
    }
    public float SqrDistance(int startIndex, int endIndex)
    {
        Debug.Assert(startIndex >= 0 && startIndex < xsize * zsize && endIndex >= 0 && endIndex < xsize * zsize);
        int sz = startIndex / xsize;
        int sx = startIndex - sz * xsize;
        int ez = endIndex / xsize;
        int ex = endIndex - ez * xsize;
        int dx = Mathf.Abs(ex - sx);
        int dz = Mathf.Abs(ez - sz);
        return (dx * dx + dz * dz) * squareSize * squareSize;
    }
}