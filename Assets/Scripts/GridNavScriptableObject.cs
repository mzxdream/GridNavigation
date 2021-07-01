using System;
using UnityEngine;

[Serializable]
public class GridNavMoveDefData
{
    public int unitSize;
    public float maxSlope;
    public float slopeMod;
    public float[] speedMods;
    public float[] speedModMults;
}

public class GridNavScriptableObject : ScriptableObject
{
    public Vector3 bmin;
    public int xsize;
    public int zsize;
    public float squareSize;
    public float[,] cornerHeights;
    public float[,] squareTypes;
    public GridNavMoveDefData[] moveDefDatas;
}