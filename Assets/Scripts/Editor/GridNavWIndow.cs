using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GridNav;

public class GridNavWindow : EditorWindow
{
    private static readonly string navDataPath = "Assets/Config/navData.asset";
    private float squareSize = 0.2f;
    private float showAngle = 90;
    private List<Mesh> gridMeshs = null;

    [MenuItem("Tools/GridNavigation")]
    public static void OpenWindow()
    {
        var window = EditorWindow.GetWindow<GridNavWindow>();
        window.Show();
    }
    private void OnEnable()
    {
        ReloadNavData();
    }
    private void OnDisable()
    {
    }
    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("SquareSize:", GUILayout.Width(100));
        squareSize = EditorGUILayout.Slider(squareSize, 0.1f, 2.0f);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("ShowAngle:", GUILayout.Width(100));
        showAngle = EditorGUILayout.Slider(showAngle, 1.0f, 90.0f);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("烘焙", GUILayout.Width(60)))
        {
            RebuildNavMap();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }
    private void RebuildNavMap()
    {
        var meshObjManager = new GridNavMeshObjManager();
        meshObjManager.CollectMeshObjs();
        var bmin = meshObjManager.BMin;
        var bmax = meshObjManager.BMax;
        var xsize = (int)((bmax.x - bmin.x) / squareSize);
        var zsize = (int)((bmax.z - bmin.z) / squareSize);
        if (xsize < 2 || zsize < 2)
        {
            Debug.LogError("xsize or zsize is too less");
            return;
        }
        xsize &= ~1;
        zsize &= ~1;
        var squareTypeMap = new int[xsize * zsize];
        for (int z = 0; z < zsize; z++)
        {
            for (int x = 0; x < xsize; x++)
            {
                var p = bmin + new Vector3((x + 0.5f) * squareSize, 0, (z + 0.5f) * squareSize);
                meshObjManager.GetPositionHeightAndType(p, out var h, out var areaType);
                squareTypeMap[x + z * xsize] = areaType;
            }
        }
        var cornerHeightMap = new float[(xsize + 1) * (zsize + 1)];
        for (int z = 0; z <= zsize; z++)
        {
            for (int x = 0; x <= xsize; x++)
            {
                var p = bmin + new Vector3(x * squareSize, 0, z * squareSize);
                meshObjManager.GetPositionHeightAndType(p, out var h, out var areaType);
                cornerHeightMap[x + z * (xsize + 1)] = h;
            }
        }
        //save nav data
        var navData = CreateInstance<GridNavScriptableObject>();
        navData.bmin = bmin;
        navData.xsize = xsize;
        navData.zsize = zsize;
        navData.squareSize = squareSize;
        navData.squareTypeMap = squareTypeMap;
        navData.cornerHeightMap = cornerHeightMap;
        AssetDatabase.CreateAsset(navData, navDataPath);
        AssetDatabase.SaveAssets();
        //reload nav data
        ReloadNavData();
    }

    private void ReloadNavData()
    {
        var navData = AssetDatabase.LoadAssetAtPath<GridNavScriptableObject>(navDataPath);
        if (navData != null)
        {
            var navMap = new NavMap();
            navMap.Init(navData.bmin, navData.xsize, navData.zsize, navData.squareSize, navData.squareTypeMap, navData.cornerHeightMap);
            GenerateMesh(navMap);
        }
    }

    private void GenerateMesh(NavMap navMap)
    {
        var maxSlope = NavUtils.DegreesToSlope(showAngle);
        gridMeshs = new List<Mesh>();
        var verts = new List<Vector3>();
        var tris = new List<int>();
        for (int z = 0; z < navMap.ZSize; z++)
        {
            for (int x = 0; x < navMap.XSize; x++)
            {
                var squareType = navMap.GetSquareType(x, z);
                if (squareType == 1)
                {
                    continue;
                }
                var slope = navMap.GetSquareSlope(x, z);
                if (slope >= maxSlope)
                {
                    continue;
                }
                if (verts.Count > 50000)
                {
                    var gridMesh = new Mesh { vertices = verts.ToArray(), triangles = tris.ToArray() };
                    gridMesh.RecalculateNormals();
                    gridMeshs.Add(gridMesh);
                    verts.Clear();
                    tris.Clear();
                }
                var pTL = navMap.GetSquareCornerPos(x, z) + new Vector3(0, 0.001f, 0);
                var PTR = navMap.GetSquareCornerPos(x + 1, z) + new Vector3(0, 0.001f, 0);
                var pBL = navMap.GetSquareCornerPos(x, z + 1) + new Vector3(0, 0.001f, 0);
                var pBR = navMap.GetSquareCornerPos(x + 1, z + 1) + new Vector3(0, 0.001f, 0);

                var index = verts.Count;
                verts.Add(pBL);
                verts.Add(pTL);
                verts.Add(PTR);
                verts.Add(pBR);
                tris.Add(index);
                tris.Add(index + 1);
                tris.Add(index + 2);
                tris.Add(index + 2);
                tris.Add(index + 3);
                tris.Add(index);
            }
        }
        if (tris.Count > 0)
        {
            var gridMesh = new Mesh { vertices = verts.ToArray(), triangles = tris.ToArray() };
            gridMesh.RecalculateNormals();
            gridMeshs.Add(gridMesh);
        }
    }

    private void OnDrawGizmos()
    {
        if (gridMeshs != null)
        {
            Gizmos.color = Color.green;
            foreach (var mesh in gridMeshs)
            {
                Gizmos.DrawMesh(mesh);
            }
        }
    }
}