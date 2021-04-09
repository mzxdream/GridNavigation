using UnityEngine;

class GridNavSquare
{
    public bool isBlocked;
}

public class GridNavMesh
{
    private Vector3 bmin;
    private int xsize;
    private int zsize;
    private int size;
    private float squareSize;
    private GridNavSquare[] squares;

    public int XSize { get => xsize; }
    public int ZSize { get => zsize; }
    public float SquareSize { get => squareSize; }
    public int Size { get => size; }

    public bool Init(Vector3 bmin, int xsize, int zsize, float squareSize)
    {
        if (xsize < 1 || zsize < 1 || squareSize <= 0.0f)
        {
            return false;
        }
        this.bmin = bmin;
        this.xsize = xsize;
        this.zsize = zsize;
        this.size = xsize * zsize;
        this.squareSize = squareSize;
        this.squares = new GridNavSquare[xsize * zsize];
        for (int z = 0; z < zsize; z++)
        {
            for (int x = 0; x < xsize; x++)
            {
                this.squares[x + z * xsize] = new GridNavSquare
                {
                    isBlocked = false,
                };
            }
        }
        return true;
    }
    public void Clear()
    {
    }
    public bool GetSquareXZ(int index, out int x, out int z)
    {
        z = index / xsize;
        x = index - z * xsize;
        return x >= 0 && x < xsize && z >= 0 && z < zsize;
    }
    public int GetSquareIndex(int x, int z)
    {
        if (x >= 0 && x < xsize && z >= 0 && z < zsize)
        {
            return x + z * xsize;
        }
        return -1;
    }
    public void SetSquare(int index, bool isBlocked)
    {
        Debug.Assert(index >= 0 && index < size);
        squares[index].isBlocked = isBlocked;
    }
    public Vector3 GetSquarePos(int index)
    {
        Debug.Assert(index >= 0 && index < size);
        int z = index / xsize;
        int x = index - z * xsize;
        return new Vector3(bmin.x + (x + 0.5f) * squareSize, 0, bmin.z + (z + 0.5f) * squareSize);
    }
    public bool IsSquareBlocked(int index)
    {
        Debug.Assert(index >= 0 && index < size);
        return squares[index].isBlocked;
    }
}