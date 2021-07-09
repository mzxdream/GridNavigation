using UnityEngine;

public class GridNavScriptableObject : ScriptableObject
{
    public Vector3 bmin;
    public int xsize;
    public int zsize;
    public float squareSize;
    public int[] squareTypeMap;
    public float[] cornerHeightMap;
}