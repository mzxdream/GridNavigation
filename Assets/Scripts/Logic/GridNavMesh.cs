using UnityEngine;

public class GridNavSquare
{
    public int x;
    public int z;
    public bool isBlocked;
}

public class GridNavMesh
{
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
                    x = x,
                    z = z,
                    isBlocked = false,
                };
            }
        }
        return true;
    }
    public void Clear()
    {
    }
    public GridNavSquare GetSquare(int index)
    {
        Debug.Assert(index >= 0 && index < xsize * zsize);
        return squares[index];
    }
    public GridNavSquare GetSquare(int x, int z)
    {
        Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
        return squares[x + z * xsize];
    }
    public void SetSquare(int index, bool isBlocked)
    {
        Debug.Assert(index >= 0 && index < xsize * zsize);
        squares[index].isBlocked = isBlocked;
    }
    public void SetSquare(int x, int z, bool isBlocked)
    {
        Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
        squares[x + z * xsize].isBlocked = isBlocked;
    }
    public Vector3 GetSquarePos(int index)
    {
        Debug.Assert(index >= 0 && index < xsize * zsize);
        var square = squares[index];
        return new Vector3(bmin.x + (square.x + 0.5f) * squareSize, 0, bmin.z + (square.z + 0.5f) * squareSize);
    }
    public Vector3 GetSquarePos(int x, int z)
    {
        Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
        return new Vector3(bmin.x + (x + 0.5f) * squareSize, 0, bmin.z + (z + 0.5f) * squareSize);
    }
}