using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GridNav;

class GridNavMeshObj
{
    public List<Vector3> verts;
    public List<int> tris;
    public int areaType;
}

public class GridNavMeshObjManager
{
    private List<GridNavMeshObj> meshObjs;
    private Vector3 bmin;
    private Vector3 bmax;

    public Vector3 BMin { get => bmin; }
    public Vector3 BMax { get => bmax; }

    public void CollectMeshObjs()
    {
        meshObjs = new List<GridNavMeshObj>();
        MeshFilter[] mfs = GameObject.FindObjectsOfType<MeshFilter>();
        for (int i = 0; i < mfs.Length; ++i)
        {
            var mf = mfs[i];
            var o = mf.gameObject;
            if ((GameObjectUtility.GetStaticEditorFlags(o) & StaticEditorFlags.NavigationStatic) == 0)
            {
                continue;
            }
            var meshObj = new GridNavMeshObj
            {
                verts = new List<Vector3>(),
                tris = new List<int>(),
                areaType = GameObjectUtility.GetNavMeshArea(o),
            };
            Mesh m = mf.sharedMesh;
            for (int j = 0; j < m.vertices.Length; j++)
            {
                meshObj.verts.Add(mf.transform.TransformPoint(m.vertices[j]));
            }
            for (int material = 0; material < m.subMeshCount; material++)
            {
                int[] triangles = m.GetTriangles(material);
                for (int j = 0; j < triangles.Length; j++)
                {
                    meshObj.tris.Add(triangles[j]);
                }
            }
            meshObjs.Add(meshObj);
        }
        //terrain
        Terrain terrainObj = GameObject.FindObjectOfType<Terrain>();
        if (terrainObj)
        {
            var o = terrainObj.gameObject;
            if ((GameObjectUtility.GetStaticEditorFlags(o) & StaticEditorFlags.NavigationStatic) == 0)
            {
                return;
            }
            var meshObj = new GridNavMeshObj
            {
                verts = new List<Vector3>(),
                tris = new List<int>(),
                areaType = GameObjectUtility.GetNavMeshArea(o),
            };
            var terrain = terrainObj.terrainData;
            var terrainPos = terrainObj.GetPosition();
            int w = terrain.heightmapResolution;
            int h = terrain.heightmapResolution;
            Vector3 meshScale = terrain.size;
            int tRes = 1;
            meshScale = new Vector3(meshScale.x / (w - 1) * tRes, meshScale.y, meshScale.z / (h - 1) * tRes);
            float[,] tData = terrain.GetHeights(0, 0, w, h);

            w = (w - 1) / tRes + 1;
            h = (h - 1) / tRes + 1;
            Vector3[] tVertices = new Vector3[w * h];
            int[] tPolys = new int[(w - 1) * (h - 1) * 6];
            // Build vertices and UVs
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tVertices[y * w + x] = Vector3.Scale(meshScale, new Vector3(y, tData[x * tRes, y * tRes], x)) + terrainPos;
                }
            }
            int index = 0;
            // Build triangle indices: 3 indices into vertex array for each triangle
            for (int y = 0; y < h - 1; y++)
            {
                for (int x = 0; x < w - 1; x++)
                {
                    // For each grid cell output two triangles
                    tPolys[index++] = (y * w) + x + 1;
                    tPolys[index++] = ((y + 1) * w) + x;
                    tPolys[index++] = (y * w) + x;

                    tPolys[index++] = (y * w) + x + 1;
                    tPolys[index++] = ((y + 1) * w) + x + 1;
                    tPolys[index++] = ((y + 1) * w) + x;
                }
            }
            for (int i = 0; i < tVertices.Length; i++)
            {
                meshObj.verts.Add(tVertices[i]);
            }
            // Write triangles
            for (int i = 0; i < tPolys.Length; i++)
            {
                meshObj.tris.Add(tPolys[i]);
            }
            meshObjs.Add(meshObj);
        }
        bmin = Vector3.positiveInfinity;
        bmax = Vector3.negativeInfinity;
        foreach (var meshObj in meshObjs)
        {
            foreach (var vert in meshObj.verts)
            {
                bmin = Vector3.Min(bmin, vert);
                bmax = Vector3.Max(bmax, vert);
            }
        }
    }

    public bool GetPositionHeightAndType(Vector3 p, out float height, out int areaType)
    {
        height = float.NegativeInfinity;
        areaType = 1;
        if (meshObjs == null)
        {
            return false;
        }
        bool isFound = false;
        foreach (var m in meshObjs)
        {
            for (int i = 0; i < m.tris.Count; i += 3)
            {
                var a = m.verts[m.tris[i]];
                var b = m.verts[m.tris[i + 1]];
                var c = m.verts[m.tris[i + 2]];
                if (NavMathUtils.ClosestHeightPointTriangle(p, a, b, c, out var h) && h > height)
                {
                    isFound = true;
                    height = h;
                    areaType = m.areaType;
                }
            }
        }
        if (!isFound)
        {
            height = p.y;
        }
        return isFound;
    }
}