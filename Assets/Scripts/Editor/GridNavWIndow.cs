using UnityEngine;
using UnityEditor;
using GridNav;

public class GridNavWindow : EditorWindow
{
    private static readonly string navDataPath = "Assets/Config/navData.asset";
    private NavMap navMap;

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
    }
    private void LoadNavData()
    {
        var navData = AssetDatabase.LoadAssetAtPath<GridNavScriptableObject>();
        if (navData != null)
        {
            navMap = new NavMap();
        }
    }
    private void SaveNavData()
    {
    }
}