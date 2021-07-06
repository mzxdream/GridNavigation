using System.Collections.Generic;
using UnityEngine;
using GridNav;

[ExecuteInEditMode]
public class GridNavShow : MonoBehaviour
{
    private List<Mesh> gridMeshs = null;
    private bool isShow = false;

    public void GenerateMesh(NavMap navMap, float maxAngle)
    {
        gridMeshs = new List<Mesh>();
        if (navMap == null)
        {
            return;
        }
        var maxSlope = NavUtils.DegreesToSlope(maxAngle);
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
    public void Show(bool isShow)
    {
        this.isShow = isShow;
    }
    private void OnDrawGizmos()
    {
        if (gridMeshs == null || !isShow)
        {
            return;
        }
        foreach (var mesh in gridMeshs)
        {
            Gizmos.color = new Color(0x0, 0xFF, 0xFF);
            Gizmos.DrawMesh(mesh);
            Gizmos.color = Color.green;
            Gizmos.DrawWireMesh(mesh);
        }
    }
}