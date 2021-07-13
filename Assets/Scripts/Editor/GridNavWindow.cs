using UnityEngine;
using UnityEditor;
using GridNav;

public class GridNavWindow : EditorWindow
{
    private static readonly string navDataPath = "Assets/Config/navData.asset";
    private float squareSize = 0.2f;
    private float showAngle = 60.0f;

    [MenuItem("Tools/GridNavigation")]
    public static void OpenWindow()
    {
        var window = GetWindow<GridNavWindow>();
        window.minSize = new Vector2(300, 50);
        window.Show();
    }
    private void Awake()
    {
        ReloadNavData();
    }
    private void OnDestroy()
    {
        GridNavMapShow.Instance.ClearMeshs();
    }
    private void OnEnable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }
    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("SquareSize:", GUILayout.Width(100));
        squareSize = EditorGUILayout.Slider(squareSize, 0.1f, 5.0f);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("ShowAngle:", GUILayout.Width(100));
        showAngle = EditorGUILayout.Slider(showAngle, 1.0f, 90.0f);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Bake", GUILayout.Width(60)))
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
        // xsize zsize must be even number
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
        Debug.Log("Bake Finished! squareSize:" + squareSize + " xsize:" + xsize + " zsize:" + zsize);
    }

    private void ReloadNavData()
    {
        var navData = AssetDatabase.LoadAssetAtPath<GridNavScriptableObject>(navDataPath);
        if (navData != null)
        {
            squareSize = navData.squareSize;
            var navMap = new NavMap();
            navMap.Init(navData.bmin, navData.xsize, navData.zsize, navData.squareSize, navData.squareTypeMap, navData.cornerHeightMap);
            GridNavMapShow.Instance.GenerateMeshs(navMap, showAngle);
        }
    }
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
        {
            GridNavMapShow.Instance.ClearMeshs();
        }
        else if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.EnteredPlayMode)
        {
            ReloadNavData();
        }
    }
}