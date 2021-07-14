using System.Collections.Generic;
using UnityEngine;
using GridNav;

public class GridNavMapShow
{
    private static GridNavMapShow instance = null;
    public static GridNavMapShow Instance => instance ?? (instance = new GridNavMapShow());
    public Dictionary<int, List<Mesh>> AreaMeshes { get; private set; } = null;

    public void GenerateMeshes(NavMap navMap, float showAngle)
    {
        Debug.Assert(navMap != null);

        var maxSlope = NavUtils.DegreesToSlope(showAngle);
        var areaSquares = new Dictionary<int, List<Vector2Int>>();
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
                if (!areaSquares.TryGetValue(squareType, out var squares))
                {
                    squares = new List<Vector2Int>();
                    areaSquares.Add(squareType, squares);
                }
                squares.Add(new Vector2Int(x, z));
            }
        }
        AreaMeshes = new Dictionary<int, List<Mesh>>();
        foreach (var v in areaSquares)
        {
            var squareType = v.Key;
            var squares = v.Value;

            var meshes = new List<Mesh>();
            AreaMeshes.Add(squareType, meshes);

            const int MeshSquarePerCount = 10000;
            int meshCount = squares.Count / MeshSquarePerCount;
            for (int i = 0; i < meshCount; i++)
            {
                var vertices = new Vector3[MeshSquarePerCount * 4];
                var triangles = new int[MeshSquarePerCount * 2 * 3];
                for (int j = 0; j < MeshSquarePerCount; j++)
                {
                    var x = squares[i * MeshSquarePerCount + j].x;
                    var z = squares[i * MeshSquarePerCount + j].y;
                    CalcVerticesAndTriangles(navMap, x, z, j, ref vertices, ref triangles);
                }
                var mesh = new Mesh { vertices = vertices, triangles = triangles };
                mesh.RecalculateNormals();
                meshes.Add(mesh);
            }
            var leftCount = squares.Count - meshCount * MeshSquarePerCount;
            if (leftCount > 0)
            {
                var vertices = new Vector3[leftCount * 4];
                var triangles = new int[leftCount * 2 * 3];
                for (int j = 0; j < leftCount; j++)
                {
                    var x = squares[meshCount * MeshSquarePerCount + j].x;
                    var z = squares[meshCount * MeshSquarePerCount + j].y;
                    CalcVerticesAndTriangles(navMap, x, z, j, ref vertices, ref triangles);
                }
                var mesh = new Mesh { vertices = vertices, triangles = triangles };
                mesh.RecalculateNormals();
                meshes.Add(mesh);
            }
        }
    }
    public void ClearMeshs()
    {
        AreaMeshes = null;
    }
    private void CalcVerticesAndTriangles(NavMap navMap, int x, int z, int i, ref Vector3[] vertices, ref int[] triangles)
    {
        var height = Vector3.up * 0.01f;
        vertices[i * 4] = navMap.GetSquareCornerPos(x, z) + height; // pTL
        vertices[i * 4 + 1] = navMap.GetSquareCornerPos(x + 1, z) + height; // pTR
        vertices[i * 4 + 2] = navMap.GetSquareCornerPos(x, z + 1) + height; // pBL
        vertices[i * 4 + 3] = navMap.GetSquareCornerPos(x + 1, z + 1) + height; // pBR
        triangles[i * 2 * 3] = i * 4;
        triangles[i * 2 * 3 + 1] = i * 4 + 2;
        triangles[i * 2 * 3 + 2] = i * 4 + 3;
        triangles[i * 2 * 3 + 3] = i * 4;
        triangles[i * 2 * 3 + 4] = i * 4 + 3;
        triangles[i * 2 * 3 + 5] = i * 4 + 1;
    }
}