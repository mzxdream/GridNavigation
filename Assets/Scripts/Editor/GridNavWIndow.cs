using UnityEngine;
using UnityEditor;
using GridNav;

public class GridNavWindow : EditorWindow
{
    private static readonly string navDataPath = "Assets/Config/navData.asset";
    //private NavMap navMap;
    private float squareSize;

    [MenuItem("Tools/GridNavigation")]
    public static void OpenWindow()
    {
        var window = EditorWindow.GetWindow<GridNavWindow>();
        window.Show();
    }
    private void OnEnable()
    {
    }
    private void OnDisable()
    {
    }
    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("", GUILayout.Width(60));
        squareSize = EditorGUILayout.FloatField(squareSize, GUILayout.Width(100));
        GUILayout.Space(10);
        if (GUILayout.Button("新建", GUILayout.Width(60)))
        {
            RebuildNavMap();
        }
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
        for (int z = 0; z <= navMap.ZSize; z++)
        {
            for (int x = 0; x <= navMap.XSize; x++)
            {
                var p = bmin + new Vector3(x * squareSize, 0, z * squareSize);
                meshObjManager.GetPositionHeightAndType(p, out var h, out var areaType);
                navMap.SetCornerHeight(x, z, h);
            }
        }

    }
    private void LoadNavData()
    {
        var navData = AssetDatabase.LoadAssetAtPath<GridNavScriptableObject>(navDataPath);
        if (navData != null)
        {
            var navMap = new NavMap();
            if (!navMap.Init(navData.bmin, navData.xsize, navData.zsize, navData.squareSize, navData.squareTypeMap, navData.cornerHeightMap))
            {
                Debug.LogError("init nav map failed");
                return;
            }
            this.navMap = navMap;
        }
    }
    private void SaveNavData()
    {
    }
}